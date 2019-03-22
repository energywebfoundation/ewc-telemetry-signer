using System.Collections.Generic;
using TelemetrySigner;

namespace tests
{
    internal class MockLogger : ILogger
    {
        public List<string> LoggedMessages { get; set; }

        public MockLogger()
        {
            LoggedMessages = new List<string>();
        }

        public void Log(string msg)
        {
            LoggedMessages.Add(msg);
        }
    }
}