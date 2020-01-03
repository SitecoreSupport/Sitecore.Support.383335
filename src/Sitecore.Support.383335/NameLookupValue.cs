// <copyright file="KeyMultiLineValueField.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>

namespace Sitecore.Support.Shell.Applications.ContentEditor
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Text;
    using Sitecore.Web.UI.HtmlControls.Data;
    using Sitecore.Web.UI.Sheer;

    ///<summary>
    /// Represents name multiline value list field
    ///</summary>
    [UsedImplicitly]
    public class NameLookupValue : Sitecore.Support.Shell.Applications.ContentEditor.NameValue
    {
        #region Public properties

        /// <summary>
        /// Gets or sets the name of the field.
        /// </summary>
        /// <value>The name of the field.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="nullable" />
        /// </contract>
        [CanBeNull]
        public string FieldName
        {
            get
            {
                return GetViewStateString("FieldName");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                SetViewStateString("FieldName", value);
            }
        }

        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        /// <value>The item ID.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="nullable" />
        /// </contract>
        [CanBeNull]
        public string ItemID
        {
            get
            {
                return GetViewStateString("ItemID");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                SetViewStateString("ItemID", value);
            }
        }

        /// <summary>
        /// Gets or sets the source.
        /// </summary>
        /// <value>The source.</value>
        /// <contract>
        ///   <requires name="value" condition="not null" />
        ///   <ensures condition="nullable" />
        /// </contract>
        [CanBeNull]
        public string Source
        {
            get
            {
                return GetViewStateString("Source");
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                SetViewStateString("Source", value);
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="NameLookupValue"/> class.
        /// </summary>
        public NameLookupValue()
        {
            this.Class += " scCombobox";
        }

        #region Protected methods
        /// <summary>
        /// Gets or sets the item language.
        /// </summary>
        /// <value>The item language.</value>
        [NotNull]
        public string ItemLanguage
        {
            get
            {
                return StringUtil.GetString(ViewState["ItemLanguage"]);
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");

                ViewState["ItemLanguage"] = value;
            }
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        /// <param name="current">The current.</param>
        /// <returns>The items.</returns>
        protected virtual Item[] GetItems(Item current)
        {
            Assert.ArgumentNotNull(current, "current");

            Item[] items = null;
            using (new LanguageSwitcher(ItemLanguage))
            {
                items = LookupSources.GetItems(current, Source);
            }
            return items;
        }

        /// <summary>
        /// Gets the item header.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The item header.</returns>
        /// <contract>
        ///   <requires name="item" condition="not null" />
        ///   <ensures condition="not null" />
        /// </contract>
        [NotNull]
        protected virtual string GetItemHeader(Item item)
        {
            Assert.ArgumentNotNull(item, "item");

            string result;

            string fieldName = StringUtil.GetString(FieldName);

            if (fieldName.StartsWith("@", StringComparison.InvariantCulture))
            {
                result = item[fieldName.Substring(1)];
            }
            else if (fieldName.Length > 0)
            {
                result = item[FieldName];
            }
            else
            {
                result = item.DisplayName;
            }

            return result;
        }

        /// <summary>
        /// Gets the item value.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <contract>
        ///   <requires name="item" condition="not null" />
        ///   <ensures condition="not null" />
        /// </contract>
        [NotNull]
        protected virtual string GetItemValue(Item item)
        {
            Assert.ArgumentNotNull(item, "item");

            return item.ID.ToString();
        }

        /// <summary>
        /// Determines whether the specified item is selected.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        /// 	<c>true</c> if the specified item is selected; otherwise, <c>false</c>.
        /// </returns>
        protected virtual bool IsSelected(Item item)
        {
            Assert.ArgumentNotNull(item, "item");

            return Value == item.ID.ToString() || Value == item.Paths.LongID;
        }

        /// <summary>
        /// Gets value html control.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="value">The value.</param>
        /// <returns>The formatted value html control.</returns>
        protected override string GetValueHtmlControl(string id, string value)
        {
            HtmlTextWriter output = new HtmlTextWriter(new StringWriter());

            Item current = Sitecore.Context.ContentDatabase.GetItem(ItemID);

            Item[] list = GetItems(current);

            output.Write("<select id=\"" + id + "_value\" name=\"" + id + "_value\"" + GetControlAttributes() + ">");

            output.Write("<option" + (string.IsNullOrEmpty(value) ? " selected=\"selected\"" : string.Empty) + " value=\"\"></option>");

            foreach (Item item in list)
            {
                string displayName = this.GetItemHeader(item);

                bool selected = item.ID.ToString() == value;

                output.Write("<option " + "value=\"" + GetItemValue(item) + "\"" + (selected ? " selected=\"selected\"" : String.Empty) + ">" + displayName + "</option>");
            }

            output.Write("</select>");

            return output.InnerWriter.ToString();
        }

        /// <summary>
        /// Name html control style
        /// </summary>
        protected override string NameStyle
        {
            get
            {
                return "width:150px;background-color:lightgrey'";
            }
        }

        #endregion
    }
}
