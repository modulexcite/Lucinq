﻿namespace Lucinq.Interfaces
{
    public interface ISearchResult<T> : ISearchResult where T : class
    {
        IItemResult<T> GetPagedItems(int start, int end);

        IItemResult<T> GetTopItems();
    }

    public interface ISearchResult
    {
        /// <summary>
        /// The total matches found
        /// </summary>
        int TotalHits { get; }

        /// <summary>
        /// The elapsed time in ms for the query to execute
        /// </summary>
        long ElapsedTimeMs { get; set; }
    }
}
