﻿using System;
using Umbraco.Core;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Core.Sync;

namespace Umbraco.Web.Scheduling
{
    internal class ContentVersionCleanup : RecurringTaskBase
    {
        private readonly IRuntimeState _runtimeState;
        private readonly IProfilingLogger _logger;
        private readonly IContentVersionCleanupPolicyGlobalSettings _settings;
        private readonly IContentVersionCleanupService _cleanupService;

        public ContentVersionCleanup(
            IBackgroundTaskRunner<RecurringTaskBase> runner,
            long delayMilliseconds,
            long periodMilliseconds,
            IRuntimeState runtimeState,
            IProfilingLogger logger,
            IContentVersionCleanupPolicyGlobalSettings settings,
            IContentVersionCleanupService cleanupService)
            : base(runner, delayMilliseconds, periodMilliseconds)
        {
            _runtimeState = runtimeState ?? throw new ArgumentNullException(nameof(runtimeState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _cleanupService = cleanupService ?? throw new ArgumentNullException(nameof(cleanupService));
        }

        public override bool PerformRun()
        {
            // Globally disabled by feature flag
            if (!_settings.EnableCleanup)
            {
                _logger.Info<ContentVersionCleanup>("ContentVersionCleanup task will not run as it has been globally disabled via configuration.");
                return false;
            }

            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return true; // repeat...
            }

            switch (_runtimeState.ServerRole)
            {
                case ServerRole.Replica:
                    _logger.Debug<ContentVersionCleanup>("Does not run on replica servers.");
                    return true; // DO repeat, server role can change
                case ServerRole.Unknown:
                    _logger.Debug<ContentVersionCleanup>("Does not run on servers with unknown role.");
                    return true; // DO repeat, server role can change
                case ServerRole.Single:
                case ServerRole.Master:
                default:
                    break;
            }

            // Ensure we do not run if not main domain, but do NOT lock it
            if (!_runtimeState.IsMainDom)
            {
                _logger.Debug<ContentVersionCleanup>("Does not run if not MainDom.");
                return false; // do NOT repeat, going down
            }

            _logger.Info<ContentVersionCleanup>("Starting ContentVersionCleanup task.");

            var report = _cleanupService.PerformContentVersionCleanup(DateTime.Now);

            _logger.Info<ContentVersionCleanup>("Finished ContentVersionCleanup task. Removed {count} item(s).", report.Count);

            return true;
        }

        public override bool IsAsync => false;
    }
}
