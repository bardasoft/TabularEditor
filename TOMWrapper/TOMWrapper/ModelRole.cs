﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabularEditor.PropertyGridUI;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace TabularEditor.TOMWrapper
{
    public partial class ModelRole: IDynamicPropertyObject
    {
        [Browsable(true), DisplayName("Row Level Filters"), Category("Security")]
        public RoleRLSIndexer RowLevelSecurity { get; private set; }

        [Browsable(true), DisplayName("Metadata Permissions"), Category("Security")]
        public RolePermissionIndexer MetadataPermission { get; private set; }

        public void InitRLSIndexer()
        {
            RowLevelSecurity = new RoleRLSIndexer(this);
            MetadataPermission = new RolePermissionIndexer(this);
        }

        public override TabularNamedObject Clone(string newName, bool includeTranslations)
        {
            Handler.BeginUpdate("duplicate role");
            var tom = MetadataObject.Clone();
            //tom.IsRemoved = false;
            tom.Name = Model.Roles.MetadataObjectCollection.GetNewName(string.IsNullOrEmpty(newName) ? tom.Name + " copy" : newName);
            var r = new ModelRole(Handler, tom);
            r.InitRLSIndexer();
            Model.Roles.Add(r);

            if (includeTranslations)
            {
                r.TranslatedDescriptions.CopyFrom(TranslatedDescriptions);
                r.TranslatedDisplayFolders.CopyFrom(TranslatedDisplayFolders);
                if (string.IsNullOrEmpty(newName))
                    r.TranslatedNames.CopyFrom(TranslatedNames, n => n + " copy");
                else
                    r.TranslatedNames.CopyFrom(TranslatedNames, n => n.Replace(Name, newName));
            }

            Handler.EndUpdate();

            return r;
        }

        internal override void Undelete(ITabularObjectCollection collection)
        {
            var tom = new TOM.ModelRole();
            MetadataObject.CopyTo(tom);
            //tom.IsRemoved = false;
            MetadataObject = tom;

            base.Undelete(collection);
        }

        public ModelRoleMemberCollection Members { get; private set; }
        protected override void Init()
        {
            Members = new ModelRoleMemberCollection(Handler, this.GetObjectPath() + ".Members", MetadataObject.Members, this);
            base.Init();
        }


        public override void Delete()
        {
            base.Delete();
        }

        [Category("Security")]
        [Description("Specify domain/usernames of the members in this role. One member per line.")]
        [Editor(typeof(System.ComponentModel.Design.MultilineStringEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Members
        {
            get
            {
                return string.Join("\n", MetadataObject.Members.Select(m => m.MemberName));
            }
            set
            {
                if (MetadataObject.Members.Any(m => m is TOM.ExternalModelRoleMember))
                    throw new InvalidOperationException("This role uses External Role Members. These role members are not supported in this version of Tabular Editor.");
                if (Members == value) return;

                Handler.UndoManager.Add(new UndoFramework.UndoPropertyChangedAction(this, "Members", Members, value));
                MetadataObject.Members.Clear();
                foreach (var member in value.Split('\n'))
                {
                    MetadataObject.Members.Add(new TOM.WindowsModelRoleMember() { MemberName = member });
                }
            }
        }
        public bool Browsable(string propertyName)
        {
            switch (propertyName) {
                case "MetadataPermission": return Model.Database.CompatibilityLevel >= 1400;
                default:  return true;
            }
        }

        public bool Editable(string propertyName)
        {
            return true;
        }
    }
}
