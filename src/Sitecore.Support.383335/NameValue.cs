// <copyright file="NameValue.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>

namespace Sitecore.Support.Shell.Applications.ContentEditor
{
    using System;
    using System.Collections.Specialized;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using Sitecore.Diagnostics;
    using Sitecore.Text;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;

    /// <summary>
    /// Represents a Text field.
    /// </summary>
    [UsedImplicitly]
    public class NameValue : Input
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NameValue"/> class.
        /// </summary>
        public NameValue()
        {
            this.Activation = true;
        }

        /// <summary>
        /// Sends server control content to a provided <see cref="System.Web.UI.HtmlTextWriter"></see> object, which writes the content to be rendered on the client.
        /// </summary>
        /// <param name="output">
        /// The <see cref="System.Web.UI.HtmlTextWriter"></see> object that receives the server control content.
        /// </param>
        protected override void DoRender([NotNull] HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");

            this.SetWidthAndHeightStyle();

            output.Write("<div" + this.ControlAttributes + ">");

            this.RenderChildren(output);

            output.Write("</div>");
        }

        /// <summary>
        /// Raises the <see cref="System.Web.UI.Control.Load"></see> event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="System.EventArgs"></see> object that contains the event data.
        /// </param>
        protected override void OnLoad([NotNull] EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

            base.OnLoad(e);

            if (Sitecore.Context.ClientPage.IsEvent)
            {
                this.LoadValue();
                return;
            }

            this.BuildControl();
        }

        /// <summary>
        /// Parameters the change.
        /// </summary>
        [UsedImplicitly]
        protected void ParameterChange()
        {
            ClientPage clientPage = Sitecore.Context.ClientPage;

            if (clientPage.ClientRequest.Source == StringUtil.GetString(clientPage.ServerProperties[this.ID + "_LastParameterID"]))
            {
                string value = clientPage.ClientRequest.Form[clientPage.ClientRequest.Source];

                if (!string.IsNullOrEmpty(value))
                {
                    string input = this.BuildParameterKeyValue(string.Empty, string.Empty);

                    clientPage.ClientResponse.Insert(this.ID, "beforeEnd", input);
                }
            }

            NameValueCollection form = null;
            System.Web.UI.Page page = HttpContext.Current.Handler as System.Web.UI.Page;
            if (page != null)
            {
                form = page.Request.Form;
            }

            if (form == null)
            {
                return;
            }

            if (this.Validate(form))
            {
                clientPage.ClientResponse.SetReturnValue(true);
            }
        }

        /// <summary>
        /// Sets the modified flag.
        /// </summary>
        protected override void SetModified()
        {
            base.SetModified();

            if (this.TrackModified)
            {
                Sitecore.Context.ClientPage.Modified = true;
            }
        }

        /// <summary>
        /// Builds the control.
        /// </summary>
        private void BuildControl()
        {
            UrlString keyValue = new UrlString(this.Value);

            foreach (string key in keyValue.Parameters.Keys)
            {
                if (key.Length > 0)
                {
                    this.Controls.Add(new LiteralControl(this.BuildParameterKeyValue(key, keyValue.Parameters[key])));
                }
            }

            this.Controls.Add(new LiteralControl(this.BuildParameterKeyValue(string.Empty, string.Empty)));
        }

        /// <summary>
        /// Builds the parameter key value.
        /// </summary>
        /// <param name="key">
        /// The parameter key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The parameter key value.
        /// </returns>
        /// <contract><requires name="key" condition="not null"/><requires name="value" condition="not null"/><ensures condition="not null"/></contract>
        [NotNull]
        private string BuildParameterKeyValue([NotNull] string key, [NotNull] string value)
        {
            Assert.ArgumentNotNull(key, "key");
            Assert.ArgumentNotNull(value, "value");

            string id = GetUniqueID(this.ID + "_Param");

            Sitecore.Context.ClientPage.ServerProperties[this.ID + "_LastParameterID"] = id;

            string change = Sitecore.Context.ClientPage.GetClientEvent(this.ID + ".ParameterChange");

            string @readonly = ReadOnly ? " readonly=\"readonly\"" : string.Empty;
            string disabled = Disabled ? " disabled=\"disabled\"" : string.Empty;
            string isVertical = (IsVertical ? "</tr><tr>" : string.Empty);

            string nameControl = string.Format("<input id=\"{0}\" name=\"{1}\" type=\"text\"{2}{3} style=\"{6}\" value=\"{4}\" onchange=\"{5}\"/>", id, id, @readonly, disabled, StringUtil.EscapeQuote(key), change, NameStyle);
            string valueControl = GetValueHtmlControl(id, StringUtil.EscapeQuote(HttpUtility.UrlDecode(value)));

            string result = string.Format("<table width=\"100%\" class='scAdditionalParameters'><tr><td>{0}</td>{2}<td width=\"100%\">{1}</td></tr></table>", nameControl, valueControl, isVertical);

            return result;
        }

        /// <summary>
        /// Loads the post data.
        /// </summary>
        private void LoadValue()
        {
            if (ReadOnly || Disabled)
            {
                return;
            }

            NameValueCollection form;

            System.Web.UI.Page page = HttpContext.Current.Handler as System.Web.UI.Page;
            if (page != null)
            {
                form = page.Request.Form;
            }
            else
            {
                form = new NameValueCollection();
            }

            UrlString parameters = new UrlString();

            foreach (string field in form.Keys)
            {
                if (string.IsNullOrEmpty(field) || !field.StartsWith(this.ID + "_Param", StringComparison.InvariantCulture) || field.EndsWith("_value", StringComparison.InvariantCulture))
                {
                    continue;
                }

                string key = form[field];
                string v = form[field + "_value"];

                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                key = Regex.Replace(key, "\\W", "_");

                parameters[key] = v ?? string.Empty;
            }

            string value = parameters.ToString();
            /*
             * SITECORE SUPPORT 383335
             * The below fixes the issue 383335.
             * This issue occurs because when the content editor loads the value, if the value is not a name value pair, for example "test"
             * the content editor will simply load empty values. Then when the editor tries to navigate away from the item, the empty loaded values are submitted here.
             * The previous code would then find that "test" != "" and then would mark it as changed(even if the editor did not change anything!) and prompt then further
             * along in the code will prompt the user to save changes, which is unexpected :)
             * 
             * The best way to fix this, would be for the content editor to load the full "raw value" and submit that same "raw value", this would remove any chance of this
             * issue occurring, however, that would require a large amount of rewriting the field, so the best option I could find to patch this, is the below.
             */
            #region SITECORE SUPPORT 383335
            NameValueCollection itemNameValueCollection = Sitecore.StringUtil.GetNameValues(this.Value, '=', '&');
            NameValueCollection postedNameValueCollection = Sitecore.StringUtil.GetNameValues(value, '=', '&');
            if (this.Value == value || (itemNameValueCollection.Count == 0 && itemNameValueCollection.Count == postedNameValueCollection.Count))
            {
                return;
            }
            #endregion

            this.Value = value;
            this.SetModified();
        }

        /// <summary>
        /// Validates the specified client page.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <returns>The result of the validation.</returns>
        private bool Validate([NotNull] NameValueCollection form)
        {
            Assert.ArgumentNotNull(form, "form");

            foreach (string field in form.Keys)
            {
                if (field == null || !field.StartsWith(this.ID + "_Param", StringComparison.InvariantCulture) || field.EndsWith("_value", StringComparison.InvariantCulture))
                {
                    continue;
                }

                string key = form[field];
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (Regex.IsMatch(key, "^\\w*$"))
                {
                    continue;
                }

                SheerResponse.Alert(string.Format(Texts.TheKey0IsInvalidAKeyMayOnlyContainLettersAndNumbers, key));
                SheerResponse.SetReturnValue(false);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets value html control.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="value">The value.</param>
        /// <returns>The formatted value html control.</returns>
        protected virtual string GetValueHtmlControl(string id, string value)
        {
            string @readonly = ReadOnly ? " readonly=\"readonly\"" : string.Empty;
            string disabled = Disabled ? " disabled=\"disabled\"" : string.Empty;
            return string.Format("<input id=\"{0}_value\" name=\"{0}_value\" type=\"text\" style=\"width:100%\" value=\"{1}\"{2}{3}/>", id, value, @readonly, disabled);
        }

        /// <summary>
        /// Name html control style
        /// </summary>
        protected virtual string NameStyle
        {
            get
            {
                return "width:150px";
            }
        }

        /// <summary>
        /// Is control vertical
        /// </summary>
        protected virtual bool IsVertical
        {
            get
            {
                return false;
            }
        }

    }
}