﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucinq.Enums;
using Lucinq.Extensions;
using Lucinq.Interfaces;
using Version = Lucene.Net.Util.Version;

namespace Lucinq.Querying
{
	public partial class QueryBuilder : IQueryBuilder
	{
		#region [ Fields ]

        private Matches defaultChildrenOccur;

		private KeywordAnalyzer keywordAnalyzer;

		#endregion

		#region [ Constructors ]

		public QueryBuilder()
		{
			Queries = new Dictionary<string, QueryReference>();
			Groups = new List<IQueryBuilder>();
			Occur = Matches.Always;
			SortFields = new List<SortField>();
		}

		public QueryBuilder(IQueryBuilder parentQueryBuilder)
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
		/// Gets or sets the occurance value for the query builder
		/// </summary>
		public Matches Occur { get; set; }

		/// <summary>
		/// Gets or sets whether the query is to be case sensitive
		/// </summary>
		public bool CaseSensitive { get; set; }

		/// <summary>
		/// Gets or sets the default occur value for child queries within the builder
		/// </summary>
		public Matches DefaultChildrenOccur
		{
			get
			{
				return GetDefaultChildrenOccur();
			}
			set { defaultChildrenOccur = value; }
		}

		/// <summary>
		/// Gets the parent query builder
		/// </summary>
		public IQueryBuilder Parent { get; private set; }

		/// <summary>
		/// Gets the child queries in the builder
		/// </summary>
		public Dictionary<string, QueryReference> Queries { get; private set; }

		/// <summary>
		/// Gets the child groups in the builder
		/// </summary>
		public List<IQueryBuilder> Groups { get; private set; }

		public List<SortField> SortFields { get; private set; }

		public Sort CurrentSort { get; set; }

		/// <summary>
		/// Gets the keyword analyzer used by the keyword queries
		/// </summary>
		public KeywordAnalyzer KeywordAnalyzer { get { return keywordAnalyzer ?? (keywordAnalyzer = new KeywordAnalyzer()); } }

		#endregion

		#region [ Setup Expressions ]

		/// <summary>
		/// A simple single item setup method
		/// Usage .Where(x => x.Term("field", "value));
		/// </summary>
		/// <param name="inputExpression">The lambda expression to be executed</param>
		/// <returns>The input querybuilder</returns>
		public virtual IQueryBuilder Where(Action<IQueryBuilder> inputExpression)
		{
			inputExpression(this);
			return this;
		}

		/// <summary>
		/// A setup method to aid multiple query setup
		/// </summary>
		/// <param name="queries">Comma seperated lambda actions</param>
		/// <returns>The input querybuilder</returns>
		public virtual IQueryBuilder Setup(params Action<IQueryBuilder>[] queries)
		{
			AddQueries(queries);
			return this;
		}

	    protected void AddQueries(params Action<IQueryBuilder>[] queries)
	    {
            foreach (Action<IQueryBuilder> item in queries)
            {
                item(this);
            }
	    }

		#endregion

		#region [ Term Expressions ]

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
			Term term = GetTerm(fieldName, fieldValue, caseSensitive);
			TermQuery query = new TermQuery(term);
			SetBoostValue(query, boost);

			Add(query, occur, key);
			return query;
		}

		/// <summary>
		/// Sets up term queries for each of the values specified
		/// (usually or / and / not in depending on the occur specified)
		/// </summary>
		/// <param name="fieldName">The field name to search within</param>
		/// <param name="fieldValues">The values to match</param>
		/// <param name="occur">Whether it must, must not or should occur in the field</param>
		/// <param name="boost">A boost multiplier (1 is default / normal).</param>
		/// <param name="caseSensitive">A boolean denoting whether or not to retain case</param>
		/// <returns>The input query builder</returns>
        public virtual IQueryBuilder Terms(string fieldName, string[] fieldValues, Matches occur = Matches.NotSet, float? boost = null, bool? caseSensitive = null)
		{
			var group = Group();
			foreach (var fieldValue in fieldValues)
			{
				group.Term(fieldName, fieldValue, occur, boost, caseSensitive:caseSensitive);
			}
			return this;
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
				group.Raw(fieldName, fieldValue, occur, boost, key, KeywordAnalyzer);
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
			Term term = GetTerm(fieldName, fieldValue, caseSensitive);
			FuzzyQuery query = new FuzzyQuery(term);
			SetBoostValue(query, boost);

			Add(query, occur, key);
			return query;
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
			query.Slop = slop;

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

        public virtual NumericRangeQuery<int> NumericRange(string fieldName, int minValue, int maxValue, Matches occur = Matches.NotSet, float? boost = null, 
													int precisionStep = 1, bool includeMin = true, bool includeMax = true, string key = null)
		{
			NumericRangeQuery<int> numericRangeQuery = NumericRangeQuery.NewIntRange(fieldName, precisionStep, minValue, maxValue, includeMin, includeMax);
			SetBoostValue(numericRangeQuery, boost);
			Add(numericRangeQuery, occur, key);
			return numericRangeQuery;
		}

        public virtual NumericRangeQuery<float> NumericRange(string fieldName, float minValue, float maxValue, Matches occur = Matches.NotSet, float? boost = null,
													int precisionStep = 1, bool includeMin = true, bool includeMax = true, string key = null)
		{
			NumericRangeQuery<float> numericRangeQuery = NumericRangeQuery.NewFloatRange(fieldName, precisionStep, minValue, maxValue, includeMin, includeMax);
			SetBoostValue(numericRangeQuery, boost);
			Add(numericRangeQuery, occur, key);
			return numericRangeQuery;
		}

        public virtual NumericRangeQuery<double> NumericRange(string fieldName, double minValue, double maxValue, Matches occur = Matches.NotSet, float? boost = null,
											int precisionStep = 1, bool includeMin = true, bool includeMax = true, string key = null)
		{
			NumericRangeQuery<double> numericRangeQuery = NumericRangeQuery.NewDoubleRange(fieldName, precisionStep, minValue, maxValue, includeMin, includeMax);
			SetBoostValue(numericRangeQuery, boost);
			Add(numericRangeQuery, occur, key);
			return numericRangeQuery;
		}

        public virtual NumericRangeQuery<long> NumericRange(string fieldName, long minValue, long maxValue, Matches occur = Matches.NotSet, float? boost = null,
									int precisionStep = 1, bool includeMin = true, bool includeMax = true, string key = null)
		{
			NumericRangeQuery<long> numericRangeQuery = NumericRangeQuery.NewLongRange(fieldName, precisionStep, minValue, maxValue, includeMin, includeMax);
			SetBoostValue(numericRangeQuery, boost);
			Add(numericRangeQuery, occur, key);
			return numericRangeQuery;
		}

		#endregion

		#region [ Sort Expressions ]

		/// <summary>
		/// Sorts the results by the corresponding field
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="sortDescending"></param>
		/// <param name="sortType"></param>
		/// <returns></returns>
		public virtual IQueryBuilder Sort(string fieldName, bool sortDescending = false, int? sortType = null)
		{
			if (!sortType.HasValue)
			{
				sortType = SortField.STRING;
			}

			SortField sortField = new SortField(fieldName, sortType.Value, sortDescending);
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
			Term term = GetTerm(fieldName, fieldValue, caseSensitive);
			WildcardQuery query = new WildcardQuery(term);
			SetBoostValue(query, boost);

			Add(query, occur, key);
			return query;
		}

        public virtual IQueryBuilder WildCards(string fieldName, string[] fieldValues, Matches occur = Matches.NotSet,
								  float? boost = null, bool? caseSensitive = null)
		{
			var group = Group();
			foreach (var fieldValue in fieldValues)
			{
				group.WildCard(fieldName, fieldValue, occur, boost, caseSensitive:caseSensitive);
			}
			return this;
		}
		#endregion

		#region [ Other Expressions ]

        public virtual Query Raw(string field, string queryText, Matches occur = Matches.NotSet, float? boost = null, string key = null, Analyzer analyzer = null)
		{
			if (analyzer == null)
			{
				analyzer = new StandardAnalyzer(Version.LUCENE_29);
			}
			QueryParser queryParser = new QueryParser(Version.LUCENE_29, field, analyzer);
			Query query = queryParser.Parse(queryText);
			SetBoostValue(query, boost);
			Add(query, occur, key);
			return query;
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
			query.Boost = boost.Value;
		}

		protected virtual void SetOccurValue(IQueryBuilder inputQueryBuilder, ref Matches occur)
		{
            if (occur != Matches.NotSet)
			{
				return;
			}

			occur = inputQueryBuilder != null ? inputQueryBuilder.DefaultChildrenOccur : Matches.Always;
		}

		/// <summary>
		/// Gets the default child occur value
		/// </summary>
		/// <returns></returns>
		protected virtual Matches GetDefaultChildrenOccur()
		{
		    if (defaultChildrenOccur == Matches.NotSet)
		    {
		        defaultChildrenOccur = Matches.Always;
		    }
		    return defaultChildrenOccur;
		}

		#endregion
	}
}
