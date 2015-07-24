﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

using InstallerStudio.Common;
using InstallerStudio.Models;
using InstallerStudio.Views.Utils;
using InstallerStudio.Views.Controls;

namespace InstallerStudio.WixElements
{
  /* Используемые аттрибуты для свойств:
   * Category - обозначение категории свойства для PropertyGrid;
   * Browsable - скрытие свойства для PropertyGrid;
   * ReadOnly - свойство только для чтения в PropertyGrid;
   * Description - подсказка пользователю в PropertyGrid;
   * DataMember - для сериализации свойства.    
   * 
   * Для данных типов, при десериализации конструктор не вызывается,
   * поэтому необходимо предусмотреть метод в базовом типе с аттрибутом [OnDeserializing]
   * и в нем выполнить метод инициализации. Метод инициализации виртуальный.
   */

  #region Поддержка файлов.

  /// <summary>
  /// Поддержка файлов. Если некий элемент должен поддерживать работу с файлами,
  /// он должен реализовать этот интерфейс и генерировать соответствующие события
  /// для уведомления и воспроизводства действиями над файлами.
  /// </summary>
  interface IFileSupport
  {
    /// <summary>
    /// Вызывается при изменение информации о путях файла.
    /// </summary>
    event EventHandler<FileSupportEventArgs> FileChanged;
  }

  class FileSupportEventArgs : EventArgs
  {
    public string OldFileName { get; private set; }
    public string OldDirectory { get; private set; }
    public string ActualFileName { get; private set; }
    public string ActualDirectory { get; private set; }

    public FileSupportEventArgs(string oldFileName, string oldDirectory,
      string actualFileName, string actualDirectory)
    {
      OldFileName = oldFileName;
      OldDirectory = oldDirectory;
      ActualFileName = actualFileName;
      ActualDirectory = actualDirectory;
    }
  }

  #endregion

  [DataContract(Namespace = StringResources.Namespace)]
  abstract class WixElementBase : ChangeableObject, IWixElement, IDataErrorInfo
  {
    #region Вложенные типы.

    /// <summary>
    /// Ошибочный тип дочернего элемента.
    /// </summary>
    internal class WrongChildTypeException : Exception { }

    internal class WixElementCollection : ObservableCollection<IWixElement>
    {
      private readonly WixElementBase parent;

      public WixElementCollection(WixElementBase parent)
      {
        this.parent = parent;
      }

      protected override void InsertItem(int index, IWixElement item)
      {
        if (!parent.CheckChildType(item.GetType()))
          throw new WrongChildTypeException();

        base.InsertItem(index, item);
      }      
    }

    #endregion

    private string id;

    public WixElementBase()
    {
      Initialize();
    }

    [OnDeserializing]
    private void OnDeserializing(StreamingContext context)
    {
      Initialize();
    }

    protected virtual void Initialize()
    {
      Items = new WixElementCollection(this);
    }

    /// <summary>
    /// Проверяет, поддерживает ли текущий тип, дочерний элемент заданного типа.
    /// </summary>
    /// <param name="type">Тип дочернего элемента.</param>
    /// <returns>Истина, если поддерживает, ложь, в противном случае.</returns>
    public bool CheckChildType(Type type)
    {
      return AllowedTypesOfChildren.FirstOrDefault(v => v == type) != null;
    }

    /// <summary>
    /// Делает текущий объект нередактируемым (присваивает его свойству IsFrozen значение true).
    /// </summary>
    public void	Freeze()
    {
      IsFrozen = true;
    }

    public void Predefinition()
    {
      IsPredefined = true;
    }

    /// <summary>
    /// Возвращает разрешенные типы дочерних элементов.
    /// </summary>
    protected virtual IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return new Type[0]; }
    }

    #region IWixElement

    /// <summary>
    /// Идентификатор Wix-элемента.
    /// </summary>
    [Category(StringResources.CategoryMain)]
    [DataMember]
    [Description(StringResources.WixElementBaseIdDescription)]
    public string Id 
    {
      get { return id; }
      set { SetValue(ref id, value); }
    }

    [Browsable(false)]
    public abstract ElementsImagesTypes ImageType { get; }

    /// <summary>
    /// Дочерние элементы.
    /// </summary>
    [Browsable(false)]
    [DataMember]
    public IList<IWixElement> Items { get; private set; }

    [Browsable(false)]
    public abstract string ShortTypeName { get; }

    /// <summary>
    /// Признак зафиксированного элемента.
    /// </summary>
    [Category(StringResources.CategoryAuxiliary)]
    [ReadOnly(true)]
    [DataMember]
    [Description(StringResources.WixElementBaseIsFrozenDescription)]
    public bool IsFrozen { get; protected set; }

    [Category(StringResources.CategoryAuxiliary)]
    [ReadOnly(true)]
    [DataMember]
    [Description(StringResources.WixElementBaseIsPredefinedDescription)]
    public bool IsPredefined { get; protected set; }

    public virtual bool AvailableForRun(Type type, IWixElement rootItem)
    {
      return CheckChildType(type);
    }

    [Browsable(false)]
    public bool IsReadOnly
    {
      get { return IsFrozen || IsPredefined; }
    }

    #endregion

    #region IDataErrorInfo

    [Browsable(false)]
    public string Error
    {
      get { return string.Empty; }
    }

    public virtual string this[string columnName]
    {
      get 
      {
        string result = string.Empty;
        switch (columnName)
        {
          case "Id":
            if (string.IsNullOrEmpty(Id))
              result = "Идентификатор не должен быть пустым.";
            break;
        }

        return result;
      }
    }

    #endregion    
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixFeatureElement : WixElementBase
  {
    [DataMember]
    public string Title { get; set; }

    [DataMember]
    public string Description { get; set; }

    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();

      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixFeatureElement), 
        typeof(WixComponentElement),
        typeof(WixDbComponentElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Feature; }
    }

    public override string ShortTypeName
    {
      get { return "Feature"; }
    }

    public override bool AvailableForRun(Type type, IWixElement rootItem)
    {
      // Особое бизнес правило: компонент DbComponent должен быть один.
      if (type == typeof(WixDbComponentElement) && rootItem.Items.Descendants().FirstOrDefault(v => v.GetType() == type) != null)
        return false;

      return base.AvailableForRun(type, rootItem);
    }

    #endregion
  }

  class WixPatchFamilyElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.SqlScript; }
    }

    public override string ShortTypeName 
    { 
      get { return "PatchFamily"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixComponentElement : WixElementBase
  {
    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();
      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixFileElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.Component; }
    }

    public override string ShortTypeName
    {
      get { return "Component"; }
    }

    #endregion
  }

  class WixDbComponentElement : WixElementBase
  {
    private Type[] allowedTypesOfChildren;

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();

      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixDbTemplateElement), 
        typeof(WixSqlScriptElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.DbComponent; }
    }

    public override string ShortTypeName
    {
      get { return "DbComponent"; }
    }

    public override bool AvailableForRun(Type type, IWixElement rootItem)
    {
      // Особое бизнес правило: компонент DbTemplate должен быть один.
      if (type == typeof(WixDbTemplateElement) && rootItem.Items.Descendants().FirstOrDefault(v => v.GetType() == type) != null)
        return false;

      return base.AvailableForRun(type, rootItem);
    }

    #endregion
  }

  class WixDbTemplateElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.DbTemplate; }
    }

    public override string ShortTypeName
    {
      get { return "DbTemplate"; }
    }

    #endregion
  }

  class WixSqlScriptElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.SqlScript; }
    }

    public override string ShortTypeName
    {
      get { return "SQLScript"; }
    }

    #endregion
  }

  [DataContract(Namespace = StringResources.Namespace)]
  class WixFileElement : WixElementBase, IFileSupport
  {
    private Type[] allowedTypesOfChildren;
    private string fileName;
    private string installDirectory;

    private void OnFileChanged(string oldSource, string oldDestination,
      string newSource, string newDestination)
    {
      if (FileChanged != null)
        FileChanged(this, new FileSupportEventArgs(oldSource, oldDestination, newSource, newDestination));
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.FilePropertyEditor, WixPropertyEditorsNames.FilePropertyEditor)]
    public string FileName
    {
      get { return fileName; }
      set 
      { 
        if (fileName != value)
        {
          string oldPath = fileName;
          fileName = value;
          NotifyPropertyChanged();
          OnFileChanged(oldPath, installDirectory, fileName, installDirectory);
        }
      }
    }

    [DataMember]
    [Editor(WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor, WixPropertyEditorsNames.DirectoryComboBoxPropertyEditor)]
    public string InstallDirectory
    {
      get { return installDirectory; }
      set 
      {
        if (installDirectory != value)
        {
          string oldInstallDirectory = installDirectory;
          installDirectory = value;
          NotifyPropertyChanged();
          OnFileChanged(fileName, oldInstallDirectory, fileName, installDirectory);
        }
      }
    }

    #region WixElementBase

    protected override void Initialize()
    {
      base.Initialize();
      allowedTypesOfChildren = new Type[] 
      { 
        typeof(WixDesktopShortcutElement), 
        typeof(WixStartMenuShortcutElement)
      };
    }

    protected override IEnumerable<Type> AllowedTypesOfChildren
    {
      get { return allowedTypesOfChildren; }
    }

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.File; }
    }

    public override string ShortTypeName
    {
      get { return "File"; }
    }

    #endregion

    #region IFileSupport

    public event EventHandler<FileSupportEventArgs> FileChanged;

    #endregion
  }

  class WixDesktopShortcutElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.DesktopShortcut; }
    }

    public override string ShortTypeName
    {
      get { return "DesktopShortcut"; }
    }

    #endregion
  }

  class WixStartMenuShortcutElement : WixElementBase
  {
    #region WixElementBase

    public override ElementsImagesTypes ImageType
    {
      get { return ElementsImagesTypes.StartMenuShortcut; }
    }

    public override string ShortTypeName
    {
      get { return "StartMenuShortcut"; }
    }

    #endregion
  }
}
