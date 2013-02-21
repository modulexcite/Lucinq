﻿using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucinq.Interfaces;
using NUnit.Framework;

namespace Lucinq.UnitTests.IntegrationTests
{
	[TestFixture]
	public class BasicTests
	{
		#region [ Fields ]

		private LuceneSearch search;
		
		#endregion

		#region [ Properties ]

		[TestFixtureSetUp]
		public void Setup()
		{
			search = new  LuceneSearch(GeneralConstants.Paths.BBCIndex);
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			search.Dispose();
		}
		#endregion

		[Test]
		public void Term()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();

			queryBuilder.Term(BBCFields.Title, "africa");

			var results = ExecuteAndAssert(queryBuilder, 8);

			Assert.AreEqual(8, results.TotalHits);

			IQueryBuilder alternative = new QueryBuilder();
			alternative.Where(x => x.Term("_name", "work"));

			var results2 = search.Execute(queryBuilder.Build(), 20);
			Assert.AreEqual(results.TotalHits, results2.TotalHits);
		}

		[Test]
		public void SetupSyntax()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Setup(x => x.Term(BBCFields.Title, "africa"));

			ExecuteAndAssert(queryBuilder, 8);
		}

		[Test]
		public void SimpleOrClauseSuccessful()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();

			queryBuilder.Or
				(
					x => x.Term(BBCFields.Title, "africa"),
					x => x.Term(BBCFields.Title, "europe")
				);

			ExecuteAndAssert(queryBuilder, 12);
		}

		[Test]
		public void SimpleAndClauseSuccessful()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();

			queryBuilder.And
				(
					x => x.Term(BBCFields.Title, "africa"),
					x => x.Term(BBCFields.Title, "road")
				);

			ExecuteAndAssert(queryBuilder, 1);
		}

		[Test]
		public void RemoveAndReexecute()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();

			queryBuilder.Term(BBCFields.Title, "africa", key: "africacriteria");

			var results = ExecuteAndAssert(queryBuilder, 8);

			queryBuilder.Queries.Remove("africacriteria");
			queryBuilder.Term(BBCFields.Title, "report", key: "businesscriteria");

			Console.WriteLine("\r\nSecond Criteria");

			var results2 = ExecuteAndAssert(queryBuilder, 5);

			Assert.AreNotEqual(results.TotalHits, results2.TotalHits);
		}

		[Test]
		public void EasyOr()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Terms(BBCFields.Title, new[] {"europe", "africa"}, BooleanClause.Occur.SHOULD);
			ExecuteAndAssert(queryBuilder, 12);
		}

		/*[Test]
		public void SimpleNot()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Not().Term("_name", "home");
			var results = ExecuteAndAssert(queryBuilder, 12);
		}*/

		[Test]
		public void PhraseDistance()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Phrase(2).AddTerm(BBCFields.Title, "wildlife").AddTerm(BBCFields.Title, "africa");
			var results = ExecuteAndAssert(queryBuilder, 1);
		}

		[Test]
		public void Fuzzy()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Fuzzy(BBCFields.Title, "afric");
			var results = ExecuteAndAssert(queryBuilder, 16);
		}

		[Test]
		public void Paging()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Setup(x => x.WildCard(BBCFields.Description, "a*"));

			var results = ExecuteAndAssertPaged(queryBuilder, 902, 0, 10);
			var documents = results.GetPagedDocuments(0, 10);
			Assert.AreEqual(10, documents.Count);

			var results2 = ExecuteAndAssertPaged(queryBuilder, 902, 1, 11);
			var documents2 = results2.GetPagedDocuments(1, 11);
			Assert.AreEqual(10, documents2.Count);

			Assert.AreEqual(documents2[1].GetValues(BBCFields.Title)[0], documents[2].GetValues(BBCFields.Title)[0]);
		}

		[Test]
		public void Sorting()
		{
			throw new NotImplementedException("Needs writing");
		}

		[Test]
		public void Range()
		{
			throw new NotImplementedException("Needs writing");
		}

		[Test]
		public void EasyAnd()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Terms(BBCFields.Title, new[] { "africa", "road" }, occur: BooleanClause.Occur.MUST);
			ExecuteAndAssert(queryBuilder, 1);
		}

		[Test]
		public void WildCard()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Setup(x => x.WildCard(BBCFields.Description, "a*"));

			ExecuteAndAssert(queryBuilder, 902);
		}

		[Test]
		public void ChainedTerms()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Setup
				(
					x => x.WildCard(BBCFields.Description, "a*"),
					x => x.Term(BBCFields.Description, "police")
				);

			ExecuteAndAssert(queryBuilder, 17);
		}

		[Test]
		public void Group()
		{
			IQueryBuilder queryBuilder = new QueryBuilder();
			queryBuilder.Setup
				(
					x => x.WildCard("_name", "a*"),
					x => x.Term("_name", "work"),
					x => x.Group().Setup
							(
								y => y.Term("_name", "work")
							)
				);

			ExecuteAndAssert(queryBuilder, 4);

			throw new NotImplementedException("Needs finishing");
		}

		private LuceneSearchResult ExecuteAndAssert(IQueryBuilder queryBuilder, int numberOfHitsExpected)
		{
			var result = search.Execute(queryBuilder, 20);


			foreach (Document document in result.GetTopDocuments())
			{
				Console.WriteLine(document.GetValues(BBCFields.Title)[0]);
			}

			Assert.AreEqual(numberOfHitsExpected, result.TotalHits);
			
			return result;
		}


		private LuceneSearchResult ExecuteAndAssertPaged(IQueryBuilder queryBuilder, int numberOfHitsExpected, int start, int end)
		{
			// Search = new LuceneSearch(GeneralConstants.Paths.BBCIndex);
			var result = search.Execute(queryBuilder, 5);
			List<Document> documents = result.GetPagedDocuments(start, end);
			foreach (Document document in documents)
			{
				Console.WriteLine(document.GetValues(BBCFields.Title)[0]);
			}

			Assert.AreEqual(numberOfHitsExpected, result.TotalHits);

			return result;
		}
	}
}