﻿using System;

namespace RechargeTools.Infrastructure
{
    /// <summary>
    /// Allows action to be executed when it is disposed
    /// </summary>
    public struct ActionDisposable : IDisposable
    {
        private readonly Action _action;

        public static readonly ActionDisposable Empty = new ActionDisposable(() => { });

        public ActionDisposable(Action action)
        {
            Guard.NotNull(action, nameof(action));

            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}