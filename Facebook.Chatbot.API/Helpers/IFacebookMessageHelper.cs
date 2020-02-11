using System.Threading.Tasks;

namespace Facebook.Chatbot.API.Helpers
{
    public interface IFacebookMessageHelper
    {
        void ReceivedAuthentication(dynamic messagingEvent);
        Task ReceivedDeliveryConfirmation(dynamic messagingEvent);
        Task ReceivedPostback(dynamic messagingEvent);
        Task ReceivedMessageRead(dynamic messagingEvent);
        Task ReceivedAccountLink(dynamic messagingEvent);
        Task ReceivedMessage(dynamic messagingEvent);
        Task ReceivedMessageRefferal(dynamic messagingEvent);
    }
}