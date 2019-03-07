namespace Sitecore.Support.EmailCampaign.Cm.sitecore_modules.Shell.EmailCampaign.UI.Dialogs
{
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;
    using Sitecore.EmailCampaign.Cm;
    using Sitecore.EmailCampaign.Cm.Factories;
    using Sitecore.EmailCampaign.Model.Web.Settings;
    using Sitecore.Framework.Conditions;
    using Sitecore.Globalization;
    using Sitecore.Modules.EmailCampaign;
    using Sitecore.Modules.EmailCampaign.Core;
    using Sitecore.Modules.EmailCampaign.Core.Contacts;
    using Sitecore.Modules.EmailCampaign.Messages;
    using Sitecore.Modules.EmailCampaign.Services;
    using Sitecore.Web;
    using Sitecore.XConnect;
    using System;
    using System.Web.UI;

    public class CustomMessageBody : Page
    {
        private readonly IContactService _contactService;

        private readonly IMessageInfoFactory _messageInfoFactory;

        private readonly IExmCampaignService _exmCampaignService;

        public CustomMessageBody() : 
            this((IContactService)ServiceLocator.ServiceProvider.GetService(typeof(IContactService)), (IMessageInfoFactory)ServiceLocator.ServiceProvider.GetService(typeof(IMessageInfoFactory)), (IExmCampaignService)ServiceLocator.ServiceProvider.GetService(typeof(IExmCampaignService)))
        {

        }

        internal CustomMessageBody(IContactService contactService, IMessageInfoFactory messageInfoFactory, IExmCampaignService exmCampaignService)
        {
            Condition.Requires<IContactService>(contactService, "contactService").IsNotNull<IContactService>();
            Condition.Requires<IMessageInfoFactory>(messageInfoFactory, "messageInfoFactory").IsNotNull<IMessageInfoFactory>();
            Condition.Requires<IExmCampaignService>(exmCampaignService, "exmCampaignService").IsNotNull<IExmCampaignService>();
            this._contactService = contactService;
            this._messageInfoFactory = messageInfoFactory;
            this._exmCampaignService = exmCampaignService;
        }

        protected override void OnLoadComplete(EventArgs e)
        {
            if (!Sitecore.Context.User.IsAuthenticated)
            {
                return;
            }
            base.OnLoadComplete(e);
            if (!base.IsPostBack)
            {
                string queryString = WebUtil.GetQueryString("message");
                Util.AssertNotNullOrEmpty(queryString);
                string queryString2 = WebUtil.GetQueryString("lang");
                MessageItem messageItem;
                if (string.IsNullOrEmpty(queryString2))
                {
                    messageItem = this._exmCampaignService.GetMessageItem(Guid.Parse(queryString));
                }
                else
                {
                    Language language;
                    Language.TryParse(queryString2, out language);
                    if (language != null)
                    {
                        messageItem = this._exmCampaignService.GetMessageItem(Guid.Parse(queryString), language.Name);
                        messageItem.TargetLanguage = language;
                    }
                    else
                    {
                        messageItem = this._exmCampaignService.GetMessageItem(Guid.Parse(queryString));

                    }
                }
                Util.AssertNotNull(messageItem);
                ContactIdentifier contactIdentifier = null;
                string queryString3 = WebUtil.GetQueryString(GlobalSettings.ContactIdentifierSourceQueryStringKey);
                string queryString4 = WebUtil.GetQueryString(GlobalSettings.ContactIdentifierIdentifierQueryStringKey);
                if (!string.IsNullOrWhiteSpace(queryString3) && !string.IsNullOrWhiteSpace(queryString4))
                {
                    contactIdentifier = new ContactIdentifier(queryString3, queryString4, ContactIdentifierType.Known);
                }
                if (contactIdentifier != null)
                {
                    Contact contact = this._contactService.GetContact(contactIdentifier, new string[]
                    {
                        "Personal",
                        "Emails",
                        "PhoneNumbers"
                    });
                    messageItem.PersonalizationRecipient = contact;
#region Sitecore.Support.315407
                    MailMessageItem mailMessage = messageItem as MailMessageItem;
                    mailMessage.ContactIdentifier = contactIdentifier;
#endregion
                }
                string queryString5 = WebUtil.GetQueryString("targetItem");
                if (!string.IsNullOrEmpty(queryString5))
                {
                    Item item = new ItemUtilExt().GetItem(queryString5);
                    if (item != null)
                    {
                        WebPageMail webPageMail = messageItem as WebPageMail;
                        if (webPageMail != null)
                        {
                            webPageMail.TargetItem = item;
                            SetDevice(webPageMail);
                        }
                    }
                }
                MessageInfo messageInfo = this._messageInfoFactory.GetMessageInfo(messageItem);
                messageInfo.FillContentEditorInfo();
                base.Response.Write(messageInfo.Body);
            }
        }

        private static void SetDevice(WebPageMail webPageMail)
        {
            string queryString = WebUtil.GetQueryString("deviceId");
            if (string.IsNullOrEmpty(queryString))
            {
                return;
            }
            Item item = new ItemUtilExt().GetItem(queryString);
            if (item != null)
            {
                webPageMail.TargetDevice = new DeviceItem(item);
            }
        }
    }
}