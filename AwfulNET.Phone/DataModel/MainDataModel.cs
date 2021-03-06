﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AwfulNET.Views;
using AwfulNET.Common;
using AwfulNET.Core.Feeds;
using AwfulNET.Core.Rest;
using AwfulNET.Core;
using AwfulNET.Core.Common;

namespace AwfulNET.DataModel
{
    public class MainDataModel : AwfulDataGroupBase
    {
        public const string DATATYPE_ARTICLE = "article";
        public const string DATATYPE_THREAD = "item";
        public const string DATATYPE_MENU = "menu";
        public const string DATATYPE_FORUM = "forum";
        public const string DATATYPE_FOLDER = "folder";
        public const string DATATYPE_PM = "message";

        [Obsolete]
        private IForumAccessToken token;

        // Threads that have been linked to via other means (private messages, etc.) go here.
        private Stack<ThreadDataItem> tabHistory;

        private PrivateMessageFolderIndex messages;
        private ArticleDataGroup articles;
        private BookmarkDataGroup bookmarks;
        private ForumsIndexGroup forums;
        private PinnedForumsGroup pinned;

        public ArticleDataGroup Articles { get { return articles; } }
        public BookmarkDataGroup Bookmarks { get { return bookmarks; } }
        public ForumsIndexGroup Forums { get { return forums; } }
        public PinnedForumsGroup Pinned { get { return pinned; } }
        public PrivateMessageFolderIndex Messages { get { return messages; } }
        public Stack<ThreadDataItem> TabHistory { get { return this.tabHistory; } }

        public MainDataModel() : base(false, false)
        {
            this.Title = "Main Menu";
            this.UniqueID = "root";

            this.articles = new ArticleDataGroup();
            this.articles.Title = "Front Page";
            this.articles.Subtitle = "Your awful news feed.";
            this.articles.Group = this;
            this.articles.DataType = DATATYPE_MENU;
           
            this.bookmarks = new BookmarkDataGroup(new BookmarksFeed());
            this.bookmarks.Title = "Bookmarks";
            this.bookmarks.Subtitle = "Threads you've bookmarked go here.";
            this.bookmarks.DataType = DATATYPE_MENU;
            this.bookmarks.Group = this;

            this.tabHistory = new Stack<ThreadDataItem>();
           
            this.forums = new ForumsIndexGroup(new ForumsIndexFeed(StorageModelFactory.GetStorageModel()), pinned);
            this.forums.Title = "Forums Index";
            this.forums.Subtitle = "The Something Awful Forums.";
            this.forums.DataType = DATATYPE_MENU;
            this.forums.Group = this;
            
            this.pinned = this.forums.Pinned;
            this.pinned.Title = "Favorites";
            this.pinned.Subtitle = "Quick access to your favorite forums.";
            this.pinned.DataType = DATATYPE_MENU;
            this.pinned.Group = this;

            this.messages = new PrivateMessageFolderIndex();
            this.messages.Title = "Private Messages";
            this.messages.Subtitle = "Check your PMs here. Platinum membership required.";
            this.messages.DataType = DATATYPE_MENU;
            this.messages.Group = this;

            this.Items.Add(articles);
            this.Items.Add(bookmarks);
            this.Items.Add(pinned);
            this.Items.Add(forums);
            this.Items.Add(messages);
            this.ItemsSource = this.Items;
        }

        [Obsolete("This method is deprecated. Use UserContext.Logout instead.", true)]
        public Task<bool> LogoutAsync(ISettingsModel settings)
        {
            bool result = settings.Remove("currentUser");
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(result);
            return tcs.Task;
        }

        [Obsolete("This method is deprecated. Use UserContext.LoadUser instead.", true)]
        internal static async Task<MainDataModel> CreateAsync(string user)
        {
            var storage = StorageModelFactory.GetStorageModel();
            IForumAccessToken token = await storage.LoadAccessTokenFromStorage(user);
            MainDataModel main = null;
            if (token != null)
            {
                main = new MainDataModel();
                main.token = token;
            }

            return main;
        }

        internal void AddToHistory(ThreadDataItem thread)
        {
            tabHistory.Push(thread);
        }

        internal ThreadDataItem GetThreadDataByID(string id)
        {
            var selected = bookmarks.Items.SingleOrDefault(item => item.UniqueID.Equals(id));
            if (selected == null)
                selected = forums.Items.SelectMany(group => (group as ICommonDataGroup).Items)
                    .FirstOrDefault(item => item.UniqueID.Equals(id));

            if (selected == null)
                selected = tabHistory.FirstOrDefault(thread => thread.UniqueID.Equals(id));

            if (selected == null)
                throw new ArgumentException("Unknown item with id: " + id);

            return selected as ThreadDataItem;
        }

        internal ArticleDataItem GetArticleDataByID(string uniqueID)
        {
            var selected = articles.Items.SingleOrDefault(item => item.UniqueID.Equals(uniqueID));
            return selected as ArticleDataItem;
        }

        internal IListViewModel GetListDataByID(string uniqueID)
        {
            var selected = forums.Items.SingleOrDefault(item => item.UniqueID.Equals(uniqueID));
            return selected as IListViewModel;
        }

        internal PrivateMessageItem GetPrivateMessageByID(string uniqueID)
        {
            var selected = messages.Items
                .SelectMany(group => (group as ICommonDataGroup).Items)
                .SingleOrDefault(item => item.UniqueID.Equals(uniqueID));

            return selected as PrivateMessageItem;
        }

        #region AwfulDataGroupBase

        public override Task OnSelectedAsync(object state, IProgress<string> progress)
        {
            return OnSelectedAsyncCore(state, progress);
        }

        protected override Task OnSelectedAsyncCore(object state, IProgress<string> progress)
        {
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
        }

        public override bool CanRefresh()
        {
            return false;
        }

        protected override Task OnRefreshAsyncCore(object state, IProgress<string> progress)
        {
            throw new NotSupportedException("The main view cannot be refreshed.");
        }

        #endregion
    }
}
