﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterService.cs" company="Wild Gums">
//   Copyright (c) 2008 - 2015 Wild Gums. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace LogViewer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Catel;
    using Catel.Collections;
    using Catel.Services;
    using MethodTimer;
    using Models;

    internal class FilterService : IFilterService
    {
        #region Fields
        private readonly IDispatcherService _dispatcherService;
        private readonly FileBrowserModel _fileBrowser;
        private readonly ILogTableService _logTableService;
        #endregion

        #region Constructors
        public FilterService(IDispatcherService dispatcherService, ILogTableService logTableService,
            IFileBrowserService fileBrowserService)
        {
            Argument.IsNotNull(() => dispatcherService);
            Argument.IsNotNull(() => logTableService);

            _dispatcherService = dispatcherService;
            _logTableService = logTableService;

            Filter = new Filter();
            _fileBrowser = fileBrowserService.FileBrowserModel;
        }
        #endregion

        #region Properties
        public Filter Filter { get; set; }
        #endregion

        #region IFilterService Members
        [Time]
        private IEnumerable<LogRecord> FilterRecords(Filter filter, IEnumerable<FileNode> logFiles)
        {
            Argument.IsNotNull(() => filter);
            Argument.IsNotNull(() => logFiles);

            var templateString = filter.SearchTemplate.TemplateString;

            if (!filter.SearchTemplate.UseFullTextSearch || string.IsNullOrEmpty(templateString))
            {
                return logFiles.Where(filter.IsAcceptableTo).SelectMany(file => file.Records.ToArray()).Where(record => filter.IsAcceptableTo(record.LogEvent) && filter.IsAcceptableTo(record.Message));
            }

            var compareInfo = CultureInfo.CurrentCulture.CompareInfo;

            Func<LogRecord, bool> where = record => filter.IsAcceptableTo(record.LogEvent);
            return logFiles.Where(file => filter.IsAcceptableTo(file) && file.Records.Any()) // select only appropriate files
                .SelectMany(x => x.Records.ToArray()).Where(x => compareInfo.IndexOf(x.Message, templateString, CompareOptions.IgnoreCase) >= 0);
        }

        public void ApplyFilesFilter()
        {
            FilterSelectedFiles();

            FilterAllFiles();
        }

        public void ApplyLogRecordsFilter(FileNode fileNode = null)
        {
            var selectedNodes = _fileBrowser.SelectedItems.OfType<FileNode>().ToArray();
            if (fileNode != null && !selectedNodes.Contains(fileNode))
            {
                return;
            }

            var logRecords = _logTableService.LogTable.Records;

            var oldRecords = logRecords.ToArray();

            var filteredRecords = FilterRecords(Filter, selectedNodes).ToArray();

            _dispatcherService.Invoke(() =>
            {                
                using (logRecords.SuspendChangeNotifications())
                {
                    logRecords.ReplaceRange(filteredRecords);
                }

                foreach (var record in logRecords.Except(oldRecords))
                {
                    record.FileNode.IsExpanded = true;
                }
            });
        }

        private void FilterSelectedFiles()
        {
            var selectedItems = _fileBrowser.SelectedItems;

            var buff = selectedItems.OfType<FileNode>().ToArray();
            if (buff.Any())
            {
                selectedItems.Clear();
                foreach (var file in buff)
                {
                    if (Filter.IsAcceptableTo(file))
                    {
                        selectedItems.Add(file);
                    }
                    else
                    {
                        file.IsSelected = false;
                        file.IsItemSelected = false;
                    }
                }
            }
        }

        private void FilterAllFiles()
        {
            foreach (var file in _fileBrowser.RootDirectories.SelectMany(x => x.GetAllNestedFiles()))
            {
                var filter = Filter;
                file.IsVisible = filter.IsAcceptableTo(file);
            }

            foreach (var subRootFolders in _fileBrowser.RootDirectories.SelectMany(x => x.Directories))
            {
                subRootFolders.UpdateVisibility();
            }
        }
        #endregion
    }
}