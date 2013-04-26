﻿using System;

namespace Controll.Common
{
    public interface IActivityDelegator
    {
        void ActivityResult(Guid ticket, object result);
        void ActivityCompleted(Guid ticket, string resultMessage);
        void ActivityError(Guid ticket, string errorMessage);
        void ActivityNotify(Guid ticket, string notificationMessage);
        void ActivityStarted(Guid ticket);
    }
}