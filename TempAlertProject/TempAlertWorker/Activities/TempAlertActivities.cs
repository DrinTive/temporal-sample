using Temporalio.Activities;

namespace TempAlertWorker.Activities
{
    public class TempAlertActivities
    {

        [Activity]
        public static async Task EmailTransporterActivity(string transporterEmail)
        {
            Console.WriteLine($"Email sent to transporter: {transporterEmail}");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task CloseIssueActivity()
        {
            Console.WriteLine("Issue closed.");
            await Task.CompletedTask;
        }

        private static int _failCount = 0;

        [Activity]
        public static async Task NotifyEscalationGroupActivity(string[] emails)
        {
            if (_failCount < 2)
            {
                _failCount++;
                throw new Exception($"Simulated failure #{_failCount} on attempt to notify escalation group.");
            }

            foreach (string email in emails)
            {
                Console.WriteLine($"Escalation email sent to: {email}");
            }

            await Task.CompletedTask;
        }


        [Activity]
        public static async Task TextEscalationGroupActivity(string[] phoneNumbers)
        {
            foreach (string phone in phoneNumbers)
            {
                Console.WriteLine($"Escalation SMS sent to: {phone}");
            }

            await Task.CompletedTask;
        }

        [Activity]
        public static async Task CallTransporterActivity(string phoneNumber)
        {
            Console.WriteLine($"Calling transporter at: {phoneNumber}");
            await Task.CompletedTask;
        }
    }
}
