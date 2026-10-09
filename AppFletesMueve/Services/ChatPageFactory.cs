using Microsoft.AspNetCore.SignalR.Client;
using AppFletesMueve.Views;

namespace AppFletesMueve.Services
{
    public interface IChatPageFactory
    {
        ChatPage Create(HubConnection hub, string groupName, string userName);
    }

    public class ChatPageFactory : IChatPageFactory
    {
        public ChatPage Create(HubConnection hub, string groupName, string userName)
        {
            return new ChatPage(hub, groupName, userName);
        }
    }
}
