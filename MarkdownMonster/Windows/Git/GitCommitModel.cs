﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using LibGit2Sharp;
using MarkdownMonster.Annotations;
using MarkdownMonster.Utilities;
using Westwind.Utilities;

namespace MarkdownMonster.Windows
{
    public class GitCommitModel : INotifyPropertyChanged
    {
        
        

        public GitCommitModel(string fileOrFolder, bool commitRepository = false)
        {
            CommitRepository = commitRepository;
            Filename = fileOrFolder;
            AppModel = mmApp.Model;
            Window = AppModel.Window;

            GitHelper = new GitHelper();
            GitHelper.OpenRepository(Filename);

            GitUsername = mmApp.Configuration.Git.GitName;
            GitEmail = mmApp.Configuration.Git.GitEmail;

            ShowUserInfo = string.IsNullOrEmpty(GitUsername);
        }
        

        public string Filename
        {
            get { return _Filename; }
            set
            {
                if (value == _Filename) return;
                _Filename = value;
                OnPropertyChanged(nameof(Filename));
                
            }
        }
        private string _Filename;


        public List<RepositoryStatusItem>  StatusItems { get; set; }

        public bool CommitRepository
        {
            get { return _CommitRepository; }
            set
            {
                if (value == _CommitRepository) return;
                _CommitRepository = value;
                OnPropertyChanged(nameof(CommitRepository));

                GetRepositoryChanges();
            }
        }
        private bool _CommitRepository;

        
        public bool IncludeIgnoredFiles
        {
            get => _includeIgnoredFiles;
            set
            {
                if (value == _includeIgnoredFiles) return;
                _includeIgnoredFiles = value;
                OnPropertyChanged();

                GetRepositoryChanges();
            }
        }
        private bool _includeIgnoredFiles;


      
        public string CommitMessage
        {
            get { return _CommitMessage; }
            set
            {
                if (value == _CommitMessage) return;
                _CommitMessage = value;
                OnPropertyChanged(nameof(CommitMessage));
            }
        }
        private string _CommitMessage;


        public bool CommitAndPush
        {
            get { return _CommitAndPush; }
            set
            {
                if (value == _CommitAndPush) return;
                _CommitAndPush = value;
                OnPropertyChanged(nameof(CommitAndPush));
            }
        }
        private bool _CommitAndPush;



        public string GitUsername
        {
            get { return _GitUsername; }
            set
            {
                if (value == _GitUsername) return;
                _GitUsername = value;
                OnPropertyChanged(nameof(GitUsername));
            }
        }
        private string _GitUsername;


        public string GitEmail
        {
            get { return _GitEmail; }
            set
            {
                if (value == _GitEmail) return;
                _GitEmail = value;
                OnPropertyChanged(nameof(GitEmail));
            }
        }
        private string _GitEmail;



        public bool ShowUserInfo
        {
            get { return _ShowUserInfo; }
            set
            {
                if (value == _ShowUserInfo) return;
                _ShowUserInfo = value;
                OnPropertyChanged(nameof(ShowUserInfo));
            }
        }
        private bool _ShowUserInfo;


        public List<RepositoryStatusItem> RepositoryStatusItems
        {
            get => _repositoryStatusItems;
            set
            {
                if (Equals(value, _repositoryStatusItems)) return;
                _repositoryStatusItems = value;
                OnPropertyChanged();
            }
        }
        private List<RepositoryStatusItem> _repositoryStatusItems;

        public AppModel AppModel { get; set; }

        public MainWindow Window { get; set; }

        public GitCommitDialog CommitWindow { get; set; }

        

        public Repository Repository { get; set; }

        public GitHelper GitHelper { get; }


        #region Helpers

        public void GetRepositoryChanges()
        {
            var statuses = GitHelper.DefaultStatusesToDisplay;

            if (IncludeIgnoredFiles)
                statuses |= FileStatus.Ignored;
            
            if (CommitRepository)
                RepositoryStatusItems = GitHelper.GetRepositoryChanges(Filename, selectAll: true, includedStatuses: statuses);
            else
                RepositoryStatusItems = GitHelper.GetRepositoryChanges(Filename, Filename,includedStatuses: statuses);
        }


        public bool CommitChangesToRepository(bool pushToRemote=false)
        {
            WindowUtilities.FixFocus(CommitWindow, CommitWindow.ListChangedItems);

            CommitWindow.ShowStatus("Committing files into local Git repository...");

            var files = RepositoryStatusItems.Where(it => it.Selected).ToList();

            if (files.Count < 1)
            {
                CommitWindow.ShowStatusError("There are no changes in this repository.");
                return false;
            }


            if (!GitHelper.Commit(files, CommitMessage, GitUsername, GitEmail) )
            {                
                CommitWindow.ShowStatusError(GitHelper.ErrorMessage);
                return false;
            }

            if (!pushToRemote)
                return true;

            CommitWindow.ShowStatus("Pushing files to the Git Remote...");
            var repo = GitHelper.OpenRepository(files[0].FullPath);

            if (!GitHelper.Push(repo.Info.WorkingDirectory))
            {
                CommitWindow.ShowStatusError(GitHelper.ErrorMessage);
                return false;
            }
            
            return true;
        }

        public bool PullChanges()
        {
            var repo = GitHelper.OpenRepository(Filename);
            if (repo == null)
            {
                Window.ShowStatus("Invalid repository path.",mmApp.Configuration.StatusMessageTimeout);
                return false;
            }

            if (!GitHelper.Pull(repo.Info.WorkingDirectory))
            {
                CommitWindow.ShowStatusError(GitHelper.ErrorMessage);
                return false;
            }

            return true;
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public enum GitCommitFormModes
    {
        ActiveDocument,
        Folder
    }

    
}
