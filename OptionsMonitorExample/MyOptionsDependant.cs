using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace OptionsMonitorExample
{
    public class MyOptionsDependant
    {
        #region Constructors

        public MyOptionsDependant(IOptionsMonitor<MyOptions> optionsMonitor)
        {
            this.OptionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

            this.ChangeListener = optionsMonitor.OnChange(this.OnMyOptionsChange);
        }

        #endregion

        #region Properties

        protected internal virtual IDisposable ChangeListener { get; }
        public virtual IList<DateTime> ChangeTimestamps { get; } = new List<DateTime>();
        protected internal virtual IOptionsMonitor<MyOptions> OptionsMonitor { get; }

        #endregion

        #region Methods

        protected internal virtual void OnMyOptionsChange(MyOptions options, string name)
        {
            this.ChangeTimestamps.Add(DateTime.UtcNow);
        }

        #endregion

        #region Other members

        ~MyOptionsDependant()
        {
            this.ChangeListener?.Dispose();
        }

        #endregion
    }
}
