﻿using System;
using System.Collections.Generic;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucinq.Adapters;
using Lucinq.Core.Enums;
using Lucinq.Core.Interfaces;
using Lucinq.Core.Querying;
using Lucinq.Extensions;
using Lucinq.Interfaces;

namespace Lucinq.Querying
{
    public partial class QueryBuilder : AbstractQueryBuilder<IQueryBuilder>, IQueryBuilder
	{
		#region [ Fields ]

		private KeywordAnalyzer keywordAnalyzer;

		#endregion

		#region [ Constructors ]

		public QueryBuilder()
		{
			SortFields = new List<SortField>();
		}

		public QueryBuilder(QueryBuilder parentQueryBuilder)
			: this()
		{
			Parent = parentQueryBuilder;
		}

        public QueryBuilder(params Action<IQueryBuilder>[] queries)
            : this()
        {
            AddQueries(queries);
        }

		#endregion

		#region [ Properties ]

        /// <summary>
        /// Gets the sort fields
        /// </summary>
		public List<SortField> SortFields { get; private set; }

        /// <summary>
        /// Gets the current sort
        /// </summary>
		public Sort CurrentSort { get; set; }

		/// <summary>
		/// Gets the keyword analyzer used by the keyword queries
		/// </summary>
		public KeywordAnalyzer KeywordAnalyzer { get { return keywordAnalyzer ?? (keywordAnalyzer = new KeywordAnalyzer()); } }

		public Filter CurrentFilter { get; private set; }

		#endregion

		#region [ Term Expressions ]

        public virtual PrefixQuery PrefixedWith(string fieldName, string fieldValue, Matches occur = Matches.NotSet, float? boost = null, String key = null, bool? caseSensitive = null)
		{
            var adapter = new PrefixQueryAdapter();
            return AddFieldValueQuery(adapter, fieldName, fieldValue, occur, boost, key, caseSensitive);

		}

		/// <summary>
		/// Sets up and adds a term query object allowing the search for an explcit term in the field
		/// Note: Wildcards should use the wildcard query type.
		/// </summary>
		/// <param name="fieldName">The field name to search within</param>
		/// <param name="fieldValue">The value to match</param>
		/// <param name="occur">Whether it must, must not or should occur in the field</param>
		/// <param name="boost">A boost multiplier (1 is default / normal).</param>
		/// <param name="key">The dictionary key to allow reference beyond the initial scope</param>
		/// <param name="caseSensitive">A boolean denoting whether or not to retain case</param>
		/// <returns>The generated term query</returns>
        public virtual TermQuery Term(string fieldName, string fieldValue, Matches occur = Matches.NotSet, float? boost = null, string key = null, bool? caseSensitive = null)
		{
		    var adapter = new TermQueryAdapter();
		    return AddFieldValueQuery(adapter, fieldName, fieldValue, occur, boost, key, caseSensitive);
		}

        public virtual IQueryBuilder Terms(string fieldName, string[] fieldValues, Matches occur = Matches.NotSet, float? boost = null, string key = null, bool? caseSensitive = null)
        {
            var adapter = new TermQueryAdapter();
            return AddFieldValuesQueries(adapter, fieldName, fieldValues, occur, boost, caseSensitive);
        }

		#endregion

		#region [ Keywords ]

        public virtual Query Keyword(string fieldName, string fieldValue, Matches occur = Matches.NotSet, float? boost = null, string key = null,
		                     bool? caseSensitive = null)
		{
			if (!caseSensitive.HasValue || !caseSensitive.Value)
			{
				fieldValue = fieldValue.ToLower();
			}
			return Raw(fieldName, fieldValue, occur, boost, key, KeywordAnalyzer);
		}

        public virtual IQueryBuilder Keywords(string fieldName, string[] fieldValues, Matches occur = Matches.NotSet, float? boost = null, string key = null,
		                      bool? caseSensitive = null)
		{
			var group = Group();
			foreach (var fieldValue in fieldValues)
			{
				group.Keyword(fieldName, fieldValue, occur, boost, key, caseSensitive);
			}
			return this;
		}

		#endregion

		#region [ Fuzzy Expressions ]

		/// <summary>
		/// Sets up and adds a fuzzy query object allowing the search for an explcit term in the field
		/// </summary>
		/// <param name="fieldName">The field name to search within</param>
		/// <param name="fieldValue">The value to match</param>
		/// <param name="occur">Whether it must, must not or should occur in the field</param>
		/// <param name="boost">A boost multiplier (1 is default / normal).</param>
		/// <param name="key">The dictionary key to allow reference beyond the initial scope</param>
		/// <param name="caseSensitive">A boolean denoting whether or not to retain case</param>
		/// <returns>The generated fuzzy query object</returns>
        public virtual FuzzyQuery Fuzzy(string fieldName, string fieldValue, Matches occur = Matches.NotSet, float? boost = null, string key = null, bool? caseSensitive = null)
		{
            var adapter = new FuzzyQueryAdapter();
            return AddFieldValueQuery(adapter, fieldName, fieldValue, occur, boost, key, caseSensitive);
		}

		#endregion

		#region [ Phrase Expressions ]

		/// <summary>
		/// Sets up and adds a phrase query object allowing the search for an explcit term in the field
		/// To add terms, use the AddTerm() query extension
		/// </summary>
		/// <param name="occur">Whether it must, must not or should occur in the field</param>
		/// <param name="slop">The allowed distance between the terms</param>
		/// <param name="boost">A boost multiplier (1 is default / normal).</param>
		/// <param name="key">The dictionary key to allow reference beyond the initial scope</param>
		/// <returns>The generated phrase query object</returns>
        public virtual PhraseQuery Phrase(int slop, float? boost = null, Matches occur = Matches.NotSet, string key = null)
		{
			PhraseQuery query = new PhraseQuery();

			SetBoostValue(query, boost);
			query.SetSlop(slop);

            Add(query, occur, key);
			return query;
		}

		/// <summary>
		/// Adds a phrase query with a number of pre-specified values
		/// </summary>
		/// <param name="fieldName">The field name to query</param>
		/// <param name="fieldValues">The array of field values</param>
		/// <param name="slop">The distance between values</param>
		/// <param name="occur">The occurance for the query</param>
		/// <param name="boost">The boost value for the query</param>
		/// <param name="caseSensitive">A boolean denoting whether or not to retain case</param>
		/// <returns>The input query builder</returns>
        public virtual IQueryBuilder Phrase(string fieldName, string[] fieldValues, int slop, Matches occur = Matches.NotSet, float? boost = null, bool? caseSensitive = null)
		{
			PhraseQuery phrase = Phrase(slop, boost, occur);
			foreach (var fieldValue in fieldValues)
			{
				phrase.AddTerm(this, fieldName, fieldValue, caseSensitive);
			}
			return this;
		}

		#endregion

		#region [ Range Expressions ]

		public virtual TermRangeQuery TermRange(string fieldName, string rangeStart, string rangeEnd, bool includeLower = true, bool includeUpper = true,
                                        Matches occur = Matches.NotSet, float? boost = null, string key = null, bool? caseSensitive = null)
		{
			if (caseSensitive.HasValue)
			{
				if (!caseSensitive.Value)
				{
					rangeStart = rangeStart.ToLowerInvariant();
					rangeEnd = rangeEnd.ToLowerInvariant();
				}
			}
			else if(!CaseSensitive)
			{
				rangeStart = rangeStart.ToLowerInvariant();
				rangeEnd = rangeEnd.ToLowerInvariant();
			}
			TermRangeQuery query = new TermRangeQuery(QueryParser.Escape(fieldName), rangeStart, rangeEnd, includeLower, includeUpper);
			SetBoostValue(query, boost);
            Add(query, occur, key);
			return query;
		}

		public virtual void Filter(Filter filter)
		{
			Add(filter);
		}
        
		#endregion

		#region [ Sort Expressions ]


        /// <summary>
        /// A convenience helper for sorting to make it more readable.
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="sortDescending"></param>
        /// <param name="sortType"></param>
        /// <returns></returns>
	    public virtual IQueryBuilder Sort(string fieldName, bool sortDescending = false, SortType sortType = SortType.String)
        {
            int sortValue = (int)sortType;

            SortField sortField = new SortField(fieldName, sortValue, sortDescending);
            SortFields.Add(sortField);
            return this;
	    }

		#endregion

		#region [ Wildcard Expressions ]

		/// <summary>
		/// Sets up and adds a wildcard query object allowing the search for an explcit term in the field
		/// </summary>
		/// <param name="fieldName">The field name to search within</param>
		/// <param name="fieldValue">The value to match</param>
		/// <param name="occur">Whether it must, must not or should occur in the field</param>
		/// <param name="boost">A boost multiplier (1 is default / normal).</param>
		/// <param name="key">The dictionary key to allow reference beyond the initial scope</param>
		/// <param name="caseSensitive"></param>
		/// <returns>The generated wildcard query object</returns>
        public virtual WildcardQuery WildCard(string fieldName, string fieldValue, Matches occur = Matches.NotSet, float? boost = null, string key = null, bool? caseSensitive = null)
		{
		    var adapter = new WildcardQueryAdapter();
            return AddFieldValueQuery(adapter, fieldName, fieldValue, occur, boost, key, caseSensitive);
		}

        public virtual IQueryBuilder WildCards(string fieldName, string[] fieldValues, Matches occur = Matches.NotSet,
								  float? boost = null, bool? caseSensitive = null)
		{
            var adapter = new WildcardQueryAdapter();
            return AddFieldValuesQueries(adapter, fieldName, fieldValues, occur, boost, caseSensitive);
		}

		#endregion

		#region [ Other Expressions ]

        public virtual Query Raw(string field, string queryText, Matches occur = Matches.NotSet, float? boost = null, string key = null, Analyzer analyzer = null)
		{
			if (analyzer == null)
			{
                analyzer = new StandardAnalyzer(CurrentVersion);
			}

            QueryParser queryParser = new QueryParser(CurrentVersion, field, analyzer);
			Query query = queryParser.Parse(queryText);
			SetBoostValue(query, boost);
			Add(query, occur, key);
			return query;
		}

		#endregion

        #region [ Build Methods ]

        private void Add(Filter filter)
        {
            CurrentFilter = filter;
        }

        /// <summary>
        /// Builds the query
        /// </summary>
        /// <returns>The query built from the queries and groups that have been added</returns>
        public virtual Query Build()
        {
            IBooleanQueryAdapter<BooleanQuery> booleanQueryAdapter = new BooleanQueryAdapter(this);

            BuildSort();

            return booleanQueryAdapter.GetQuery();
        }

        public virtual void BuildSort()
        {
            if (SortFields.Count == 0)
            {
                return;
            }
            CurrentSort = new Sort(SortFields.ToArray());
        }

        #endregion

		#region [ Helper Methods ]

		protected virtual Term GetTerm(string field, string value, bool? caseSensitive = null)
		{
			if (caseSensitive.HasValue)
			{
				if (!caseSensitive.Value)
				{
					value = value.ToLowerInvariant();
				}
			}
			else if (!CaseSensitive)
			{
				value = value.ToLowerInvariant();
			}
			return new Term(field, value);
		}

		protected virtual void SetBoostValue(Query query, float? boost)
		{
			if (!boost.HasValue)
			{
				return;
			}
			query.SetBoost(boost.Value);
		}

		#endregion

        protected override IQueryReference GetNativeReference<TNative>(TNative query, Matches occur, string key)
        {
            var nativeReference = new NativeQueryReference();
            nativeReference.Query = query as Query;
            nativeReference.Occur = occur;
            return nativeReference;
        }
	}
}
