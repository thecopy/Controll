﻿using System;

namespace Controll.Common.ViewModels
{
    public class ActivityInvocationLogMessageViewModel
    {
        public string Message { get; set; }
        public DateTime Date { get; set; }
        public ActivityMessageType MessageType { get; set; }
    }
}