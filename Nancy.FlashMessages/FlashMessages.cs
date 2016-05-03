﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Bootstrapper;
using Nancy.FlashMessages.Extensions;
using Nancy.Session;
using Newtonsoft.Json;

namespace Nancy.FlashMessages
{
    /// <summary>
    /// The FlashMessages class manages lists of messages to be displayed to a user. We can 
    /// have multiple lists of messages, each identified by a different message type. By
    /// default we have "info", "warning", "success" and "danger", which correspond with
    /// the basic alert types found in the Twitter Bootstrap css framework.
    /// 
    /// The FlashMessages class uses an ISession to persist message lists, so that they
    /// can survive redirection and so forth. 
    /// 
    /// NancyContext and NancyModule extension methods can be used to easily add messages
    /// during request handling.
    /// </summary>
    public class FlashMessages
    {
        public const string Info = "info";
        public const string Warning = "warning";
        public const string Success = "success";
        public const string Danger = "danger";
        public const string Error = "danger";

        private const string SessionKey = "__fm";

        private readonly ISession _session;
        private readonly FlashMessagesConfiguration _configuration;

        /// <summary>
        /// Enables FlashMessages for the Nancy application, using the default 
        /// FlashMessagesConfigruation implementation
        /// </summary>
        /// <param name="pipelines"></param>
        public static void Enable(IPipelines pipelines)
        {
            Enable(pipelines, new FlashMessagesConfiguration());
        }

        /// <summary>
        /// Enables FlashMessages for the Nancy application, using the provided 
        /// configuration
        /// </summary>
        /// <param name="pipelines"></param>
        /// <param name="configuration"></param>
        public static void Enable(IPipelines pipelines, FlashMessagesConfiguration configuration)
        {
            pipelines.BeforeRequest.AddItemToEndOfPipeline(ctx =>
            {
                ctx.SetFlashMessages(new FlashMessages(ctx.Request.Session, configuration));
                return null;
            });

            // add item to *start* of pipeline so it executes before session cookie is saved
            pipelines.AfterRequest.AddItemToStartOfPipeline(ctx =>
            {
                // pop flash messages out of session cookie (and place in viewbag) *if* a view is associated with the response
                if (!string.IsNullOrEmpty(ctx.NegotiationContext.ViewName))
                {
                    var flashMessages = ctx.GetFlashMessages();
                    var messages = flashMessages.PopAllMessages();
                    if (messages != null && messages.Count() > 0)
                        ctx.ViewBag.FlashMessages = messages;
                }
            });
        }

        /// <summary>
        /// Retrieve the configuration
        /// </summary>
        public FlashMessagesConfiguration Configuration
        {
            get { return _configuration; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="session"></param>
        /// <param name="configuration"></param>
        public FlashMessages(ISession session, FlashMessagesConfiguration configuration)
        {
            if (session == null)
            {
                throw new ArgumentNullException("session", "You need to initialise a session provider in your Nancy Bootstrapper in order to use FlashMessages");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _session = session;
            _configuration = configuration;
        }

        IDictionary<string, IList<string>> DeserializeMessages()
        {
            IDictionary<string, IList<string>> messages = null;
            var sessionValue = _session[SessionKey];
            if (sessionValue != null)
                messages = JsonConvert.DeserializeObject<IDictionary<string, IList<string>>>(sessionValue.ToString());
            return messages;
        }

        /// <summary>
        /// Adds a message to the relevant alert message list
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        public void AddMessage(string messageType, string message)
        {
            var messages = DeserializeMessages();

            if (messages == null)
            {
                messages = new Dictionary<string, IList<string>>();
            }

            if (!messages.ContainsKey(messageType))
            {
                messages[messageType] = new List<string>();
            }

            messages[messageType].Add(message);
            _session[SessionKey] = JsonConvert.SerializeObject(messages);
        }

        /// <summary>
        /// Retrieves the list of alert messages of a given type
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public IEnumerable<string> PeekMessages(string messageType)
        {
            var messages = DeserializeMessages();

            if (messages != null && messages.ContainsKey(messageType))
            {
                return messages[messageType];
            }

            return null;
        }

        /// <summary>
        /// Retrieves the list of alert messages of a given type, and removes the list
        /// from the session
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public IEnumerable<string> PopMessages(string messageType)
        {
            var messages = DeserializeMessages();

            if (messages == null || !messages.ContainsKey(messageType)) return null;

            var m = messages[messageType];
            messages.Remove(messageType);
            _session[SessionKey] = JsonConvert.SerializeObject(messages);
            return m;
        }

        /// <summary>
        /// Retrieves the list of alert messages of all types, and removes the alerts
        /// from the session
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, IList<string>> PopAllMessages()
        {
            var messages = DeserializeMessages();

            if (messages == null) return null;

            _session.Delete(SessionKey);
            return messages;
        }
    }
}