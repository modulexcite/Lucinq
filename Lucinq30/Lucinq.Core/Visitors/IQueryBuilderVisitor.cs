﻿using Lucinq.Core.Querying;

namespace Lucinq.Core.Visitors
{
    public interface IQueryBuilderVisitor
    {
        void VisitQueryBuilder(ICoreQueryBuilder queryBuilder);
    }
}