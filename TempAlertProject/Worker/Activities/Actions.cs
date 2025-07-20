using Temporalio.Activities;

namespace Worker.Activities
{
  public class Actions
    {
        private static int _failCount;

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

        [Activity]
        public static async Task UpdateTransmissionIntervalAsync(int minutes)
        {
            Console.WriteLine($"Updating interval to {minutes} minutes.");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task RevertTransmissionSettingsAsync()
        {
            Console.WriteLine("Reverting interval to default.");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task ActivateGpsAsync()
        {
            Console.WriteLine("Activating GPS.");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task DeactivateGpsAsync()
        {
            Console.WriteLine("Deactivating GPS.");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task SendEmailAsync(string recipient, string templateId)
        {
            Console.WriteLine($"Sending email to {recipient} using {templateId}.");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task<double> GetBatteryLevelAsync()
        {
            Console.WriteLine("Checking battery level.");
            return await Task.FromResult(85.0);
        }

        [Activity]
        public static async Task CallEscalationContactAsync()
        {
            Console.WriteLine("Calling escalation contact.");
            await Task.CompletedTask;
        }

        [Activity]
        public static async Task CloseStatusAsync()
        {
            Console.WriteLine("Closing alert status.");
            await Task.CompletedTask;
        }
    }
}
