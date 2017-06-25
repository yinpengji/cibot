using Microsoft.Bot.Connector;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using System;
using Autofac;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System.Threading;
using Microsoft.Bot.Builder.Internals.Fibers;

namespace LyncBot.Core
{
    public class LyncClientManager
    {
        private LyncClient _lyncClient;
        private ConversationManager _conversationManager;
        public void Initialize()
        {
            try
            {
                _lyncClient = LyncClient.GetClient();
                _lyncClient.StateChanged += StateChanged;
                if (_lyncClient.State == ClientState.SignedOut)
                {
                    _lyncClient.BeginSignIn(
                        null,
                        null,
                        null,
                        (ar) =>
                        {
                            _lyncClient.EndSignIn(ar);
                        }
                        ,
                        null);
                }
                else if (_lyncClient.State == ClientState.SignedIn)
                {
                    _conversationManager = _lyncClient.ConversationManager;
                    _conversationManager.ConversationAdded -= ConversationAdded;
                    _conversationManager.ConversationAdded += ConversationAdded;
                    HookAllExistingConversations();
                }
            }
            catch (NotStartedByUserException)
            {
                throw new Exception("Lync is not running");
            }
        }

        private void HookAllExistingConversations()
        {
            if (_conversationManager == null)
            { return; }
            foreach( var conver in _conversationManager.Conversations)
            {
                conver.ParticipantAdded -= ParticipantAdded;
                conver.ParticipantRemoved -= ParticipantRemoved;
                conver.ParticipantAdded += ParticipantAdded;
                conver.ParticipantRemoved += ParticipantRemoved;
                foreach(var participent in conver.Participants)
                {
                    if (participent.IsSelf)
                    {
                        continue;
                    }
                    var instantMessageModality =
                       participent.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality;
                    instantMessageModality.InstantMessageReceived -= InstantMessageReceived;
                    instantMessageModality.InstantMessageReceived += InstantMessageReceived;
                }
            }
        }

        private void UnHookAllExistingConversations()
        {
            if (_conversationManager == null)
            { return; }
            foreach (var conver in _conversationManager.Conversations)
            {
                conver.ParticipantAdded -= ParticipantAdded;
                conver.ParticipantRemoved -= ParticipantRemoved;
                foreach (var participent in conver.Participants)
                {
                    if (participent.IsSelf)
                    {
                        continue;
                    }
                    var instantMessageModality =
                       participent.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality;
                    instantMessageModality.InstantMessageReceived -= InstantMessageReceived;
                }
            }
        }

        private void StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            if (e.NewState == ClientState.SignedIn)
            {
                _conversationManager = _lyncClient.ConversationManager;
                _conversationManager.ConversationAdded -= ConversationAdded;
                _conversationManager.ConversationAdded += ConversationAdded;
                HookAllExistingConversations();
            }
            if (e.NewState == ClientState.SignedOut)
            {
                _conversationManager.ConversationAdded -= ConversationAdded;
                UnHookAllExistingConversations();
            }
        }

        private void ConversationAdded(object sender, ConversationManagerEventArgs e)
        {
            e.Conversation.ParticipantAdded -= ParticipantAdded;
            e.Conversation.ParticipantRemoved -= ParticipantRemoved;
            e.Conversation.ParticipantAdded += ParticipantAdded;
            e.Conversation.ParticipantRemoved += ParticipantRemoved;
            //e.Conversation.StateChanged -= Conversation_StateChanged;
            //e.Conversation.StateChanged += Conversation_StateChanged;
        }

        private void Conversation_StateChanged(object sender, ConversationStateChangedEventArgs e)
        {
            Conversation conv = sender as Conversation;
            if (conv != null)
            {
                conv.End();
            }
        }

        private void ParticipantRemoved(object sender, ParticipantCollectionChangedEventArgs e)
        {
            var participant = e.Participant;
            if (participant.IsSelf)
            {
                return;
            }
            var instantMessageModality =
               e.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality;
            instantMessageModality.InstantMessageReceived -= InstantMessageReceived;
        }

        private void ParticipantAdded(object sender, ParticipantCollectionChangedEventArgs e)
        {
            var participant = e.Participant;
            if (participant.IsSelf)
            {
                return;
            }

            var instantMessageModality =
                e.Participant.Modalities[ModalityTypes.InstantMessage] as InstantMessageModality;
            instantMessageModality.InstantMessageReceived -= InstantMessageReceived;
            instantMessageModality.InstantMessageReceived += InstantMessageReceived;
        }

        private void InstantMessageReceived(object sender, MessageSentEventArgs e)
        {
            var text = e.Text.Replace(Environment.NewLine, string.Empty);
            var conversationService = new ConversationService((InstantMessageModality)sender);
            SendToBot(conversationService, text);
        }

        private async void SendToBot(ConversationService conversationService, string text)
        {
            Activity activity = new Activity()
            {
                From = new ChannelAccount { Id = conversationService.ParticipantId, Name = conversationService.ParticipantName },
                Conversation = new ConversationAccount { Id = conversationService.ConversationId },
                Recipient = new ChannelAccount { Id = "Bot" },
                ServiceUrl = "https://skype.botframework.com",
                ChannelId = "skype",
            };

            activity.Text = text;

            using (var scope = Microsoft.Bot.Builder.Dialogs.Conversation
                .Container.BeginLifetimeScope(DialogModule.LifetimeScopeTag, builder => Configure(builder, conversationService)))
            {
                try
                {
                    scope.Resolve<IMessageActivity>
                        (TypedParameter.From((IMessageActivity)activity));
                    DialogModule_MakeRoot.Register
                        (scope, () => new Dialogs.LyncLuisDialog(scope.Resolve<PresenceService>()));
                    var postToBot = scope.Resolve<IPostToBot>();
                    await postToBot.PostAsync(activity, CancellationToken.None);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void Configure(ContainerBuilder builder, ConversationService service)
        {
            builder.Register(c => new BotToUserLync(c.Resolve<IMessageActivity>(), service.SendMessageToUser))
               .As<IBotToUser>()
               .InstancePerLifetimeScope();
            builder.Register(c => new PresenceService(_lyncClient.Self))
                .Keyed<PresenceService>(FiberModule.Key_DoNotSerialize)
                .AsSelf()
                .SingleInstance();
        }
    }
}
