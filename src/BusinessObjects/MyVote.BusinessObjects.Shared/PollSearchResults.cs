﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
#if !NETFX_CORE && !WINDOWS_PHONE && !ANDROID && !IOS
using System.Data.Entity.SqlServer;
using MyVote.Data.Entities;
#endif
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Csla;
using MyVote.BusinessObjects.Contracts;
using MyVote.BusinessObjects.Core;
using MyVote.BusinessObjects.Core.Contracts;
using MyVote.BusinessObjects.Attributes;

namespace MyVote.BusinessObjects
{
#if (!NETFX_CORE && !WINDOWS_PHONE) || ANDROID || IOS
	[System.Serializable]
#else
	[Csla.Serialization.Serializable]
#endif
	internal sealed class PollSearchResults
		: ReadOnlyBaseScopeCore<PollSearchResults>, IPollSearchResults
	{
		private const int MaximumResultCount = 50;

#if !NETFX_CORE && !WINDOWS_PHONE && !ANDROID && !IOS
		private void DataPortal_Fetch(string pollQuestion)
		{
			var now = DateTime.UtcNow;
			var stringPattern = "%" + pollQuestion.ToLower() + "%";

			//this.ProcessQueryResults(
			//	(from poll in this.Entities.MVPolls
			//	 where(poll.PollStartDate < now && poll.PollEndDate > now &&
			//		this.sqlMethods.PatIndex(stringPattern, poll.PollQuestion.ToLower()) > 0 &&
			//		(poll.PollDeletedFlag == null || !poll.PollDeletedFlag.Value))
			//	 join category in this.Entities.MVCategories on poll.PollCategoryID equals category.CategoryID
			//	 orderby category.CategoryName ascending
			//	 join popular in
			//		 (from submission in this.Entities.MVPollSubmissions
			//		  group submission by submission.PollID into submissionCount
			//		  select new
			//		  {
			//			  PollId = submissionCount.Key,
			//			  Count = submissionCount.Count()
			//		  }) on poll.PollID equals popular.PollId
			//	 orderby popular.Count descending
			//	 select new PollSearchResultsData
			//	 {
			//		 Category = category.CategoryName,
			//		 Id = poll.PollID,
			//		 ImageLink = poll.PollImageLink,
			//		 Question = poll.PollQuestion,
			//		 SubmissionCount = popular.Count
			//	 }).Take(PollSearchResults.MaximumResultCount).ToList());

			var polls = this.Entities.MVPolls
				.Where(this.SearchWhereClause.WhereClause(now, stringPattern));

			var results = (from poll in polls 
				join category in this.Entities.MVCategories on poll.PollCategoryID equals category.CategoryID
				orderby category.CategoryName ascending
				join popular in
					(from submission in this.Entities.MVPollSubmissions
					group submission by submission.PollID into submissionCount
					select new
					{
						PollId = submissionCount.Key,
						Count = submissionCount.Count()
					}) on poll.PollID equals popular.PollId
				orderby popular.Count descending
				select new PollSearchResultsData
				{
					Category = category.CategoryName,
					Id = poll.PollID,
					ImageLink = poll.PollImageLink,
					Question = poll.PollQuestion,
					SubmissionCount = popular.Count
				}).Take(PollSearchResults.MaximumResultCount).ToList();

			this.ProcessQueryResults(results);
		}

		private static System.Linq.Expressions.Expression<Func<MVPoll, bool>> WhereClause(DateTime now, string stringPattern)
		{
			return poll => (poll.PollStartDate < now && poll.PollEndDate > now &&
					SqlFunctions.PatIndex(stringPattern, poll.PollQuestion.ToLower()) > 0 &&
					(poll.PollDeletedFlag == null || !poll.PollDeletedFlag.Value));
		}

		private void DataPortal_Fetch(PollSearchResultsQueryType criteria)
		{
			var now = DateTime.UtcNow;

			if (criteria == PollSearchResultsQueryType.MostPopular)
			{
				this.ProcessQueryResults(
					(from result in
						 ((from poll in this.Entities.MVPolls
							where (poll.PollStartDate < now && poll.PollEndDate > now &&
							  (poll.PollDeletedFlag == null || !poll.PollDeletedFlag.Value))
							join category in this.Entities.MVCategories on poll.PollCategoryID equals category.CategoryID
							orderby category.CategoryName ascending
							join popular in
								(from submission in this.Entities.MVPollSubmissions
								 group submission by submission.PollID into submissionCount
								 select new
								 {
									 PollId = submissionCount.Key,
									 Count = submissionCount.Count()
								 }) on poll.PollID equals popular.PollId into pollCounts
							from pollCount in pollCounts.DefaultIfEmpty(new { PollId = poll.PollID, Count = 0 }) // Adding a default instance when empty keeps our unit tests from throwing NullRefExceptions.
							orderby pollCount.Count descending
							select new
							{
								Category = category.CategoryName,
								Id = poll.PollID,
								ImageLink = poll.PollImageLink,
								Question = poll.PollQuestion,
								Count = pollCount
							}).Take(PollSearchResults.MaximumResultCount).ToList())
					 select new PollSearchResultsData
					 {
						 Category = result.Category,
						 Id = result.Id,
						 ImageLink = result.ImageLink,
						 Question = result.Question,
						 SubmissionCount = result.Count.Count
					 }).ToList());
			}
			else if (criteria == PollSearchResultsQueryType.Newest)
			{
				this.ProcessQueryResults(
					(from result in
						 ((from poll in this.Entities.MVPolls
							where (poll.PollStartDate < now && poll.PollEndDate > now &&
								(poll.PollDeletedFlag == null || !poll.PollDeletedFlag.Value))
							join category in this.Entities.MVCategories on poll.PollCategoryID equals category.CategoryID
							orderby category.CategoryName ascending
							join counts in
								(from submission in this.Entities.MVPollSubmissions
								 group submission by submission.PollID into submissionCount
								 select new
								 {
									 PollId = submissionCount.Key,
									 Count = submissionCount.Count()
								 }) on poll.PollID equals counts.PollId into pollCounts
							from pollCount in pollCounts.DefaultIfEmpty()
							orderby poll.PollStartDate descending
							select new
							{
								Category = category.CategoryName,
								Id = poll.PollID,
								ImageLink = poll.PollImageLink,
								Question = poll.PollQuestion,
								Count = pollCount
							}).Take(PollSearchResults.MaximumResultCount).ToList())
					 select new PollSearchResultsData
					 {
						 Category = result.Category,
						 Id = result.Id,
						 ImageLink = result.ImageLink,
						 Question = result.Question,
						 SubmissionCount = (result.Count != null ? result.Count.Count : 0)
					 }).ToList());
			}
			else
			{
				throw new InvalidEnumArgumentException("criteria", (int)criteria, typeof(PollSearchResultsQueryType));
			}
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
		private void DataPortal_Fetch(PollSearchResultsByUserCriteria criteria)
		{
			var now = DateTime.UtcNow;

			if (criteria.ArePollsActive)
			{
				this.ProcessQueryResults(
					(from result in
						 ((from poll in this.Entities.MVPolls
							where (poll.PollStartDate < now && poll.PollEndDate > now &&
							poll.UserID == criteria.UserID && (poll.PollDeletedFlag == null || !poll.PollDeletedFlag.Value))
							join category in this.Entities.MVCategories on poll.PollCategoryID equals category.CategoryID
							orderby category.CategoryName ascending
							join counts in
								(from submission in this.Entities.MVPollSubmissions
								 group submission by submission.PollID into submissionCount
								 select new
								 {
									 PollId = submissionCount.Key,
									 Count = submissionCount.Count()
								 }) on poll.PollID equals counts.PollId into pollCounts
							from pollCount in pollCounts.DefaultIfEmpty()
							select new
							{
								Category = category.CategoryName,
								Id = poll.PollID,
								ImageLink = poll.PollImageLink,
								Question = poll.PollQuestion,
								Count = pollCount
							}).Take(PollSearchResults.MaximumResultCount).ToList())
					 select new PollSearchResultsData
					 {
						 Category = result.Category,
						 Id = result.Id,
						 ImageLink = result.ImageLink,
						 Question = result.Question,
						 SubmissionCount = (result.Count != null ? result.Count.Count : 0)
					 }).ToList());
			}
			else
			{
				this.ProcessQueryResults(
					(from result in
						 ((from poll in this.Entities.MVPolls
							where (poll.UserID == criteria.UserID && (poll.PollDeletedFlag == null || !poll.PollDeletedFlag.Value))
							where poll.PollEndDate < now
							join category in this.Entities.MVCategories on poll.PollCategoryID equals category.CategoryID
							orderby category.CategoryName ascending
							join counts in
								(from submission in this.Entities.MVPollSubmissions
								 group submission by submission.PollID into submissionCount
								 select new
								 {
									 PollId = submissionCount.Key,
									 Count = submissionCount.Count()
								 }) on poll.PollID equals counts.PollId into pollCounts
							from pollCount in pollCounts.DefaultIfEmpty()
							select new
							{
								Category = category.CategoryName,
								Id = poll.PollID,
								ImageLink = poll.PollImageLink,
								Question = poll.PollQuestion,
								Count = pollCount
							}).Take(PollSearchResults.MaximumResultCount).ToList())
					 select new PollSearchResultsData
					 {
						 Category = result.Category,
						 Id = result.Id,
						 ImageLink = result.ImageLink,
						 Question = result.Question,
						 SubmissionCount = (result.Count != null ? result.Count.Count : 0)
					 }).ToList());
			}
		}

		private void ProcessQueryResults(List<PollSearchResultsData> results)
		{
			var resultList = DataPortal.FetchChild<ReadOnlySwitchList<IPollSearchResultsByCategory>>();
			resultList.SwitchReadOnlyStatus();

			var pollsByCategories = new Dictionary<string, List<PollSearchResultsData>>();

			foreach (var result in results)
			{
				List<PollSearchResultsData> pollsByCategory = null;

				if (pollsByCategories.ContainsKey(result.Category))
				{
					pollsByCategory = pollsByCategories[result.Category];
				}
				else
				{
					pollsByCategory = new List<PollSearchResultsData>();
					pollsByCategories.Add(result.Category, pollsByCategory);
				}

				pollsByCategory.Add(result);
			}

			foreach (var pollDataPair in pollsByCategories)
			{
				resultList.Add(DataPortal.FetchChild<PollSearchResultsByCategory>(pollDataPair.Value));
			}

			resultList.SwitchReadOnlyStatus();
			this.SearchResultsByCategory = resultList;
		}
#endif

		public static PropertyInfo<ReadOnlySwitchList<IPollSearchResultsByCategory>> SearchResultsByCategoryProperty =
			PollSearchResults.RegisterProperty<ReadOnlySwitchList<IPollSearchResultsByCategory>>(_ => _.SearchResultsByCategory);
		public IReadOnlyListBaseCore<IPollSearchResultsByCategory> SearchResultsByCategory
		{
			get { return this.ReadProperty(PollSearchResults.SearchResultsByCategoryProperty); }
			private set { this.LoadProperty(PollSearchResults.SearchResultsByCategoryProperty, value); }
		}

#if !NETFX_CORE && !WINDOWS_PHONE && !ANDROID && !IOS
		[NonSerialized]
		private ISearchWhereClause searchWhereClause;
		[Dependency]
		public ISearchWhereClause SearchWhereClause
		{
			get { return this.searchWhereClause; }
			set { this.searchWhereClause = value; }
		}
#endif
	}
}