// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using Azure.SignalRBench.Storage;
using Microsoft.Azure.Cosmos.Table;
using Xunit;

namespace Azure.SignalRBench.Tests.StorageTest
{
    public class TableTest
    {
        [SkippableFact]
        public async Task TestTableCrud()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var table = await storage.GetTableAsync<TestEntity>(nameof(TestTableCrud).ToLower());
            var dt = DateTime.Now;
            var entity = new TestEntity
            {
                PartitionKey = dt.ToString("yyyyMMdd"),
                RowKey = dt.ToString("HHmmss"),
                Code = dt.Ticks,
                Text = dt.ToString("yyyyMMddHHmmss"),
            };

            await table.InsertAsync(entity);
            Assert.NotNull(entity.ETag);

            var retrieved = await table.GetAsync(entity.PartitionKey, entity.RowKey);
            Assert.NotNull(retrieved);
            Assert.Equal(entity, retrieved, TestEntityComparer.Instance);

            retrieved.Code++;
            await table.UpdateAsync(retrieved);
            Assert.NotEqual(entity.ETag, retrieved.ETag);

            var retrived2 = await table.GetAsync(entity.PartitionKey, entity.RowKey);
            Assert.NotNull(retrived2);
            Assert.Equal(retrieved, retrived2, TestEntityComparer.Instance);

            await Assert.ThrowsAsync<StorageException>(() => table.DeleteAsync(entity));

            await table.DeleteAsync(retrieved);
            Assert.Null(await table.GetAsync(entity.PartitionKey, entity.RowKey));
        }

        [SkippableFact]
        public async Task TestTableBatch()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var table = await storage.GetTableAsync<TestEntity>(nameof(TestTableBatch).ToLower());
            const int Count = 30;
            var dt = DateTime.Now;
            var pk = dt.ToString("yyyyMMddHHmmss");
            var entities = (from i in Enumerable.Range(0, Count)
                            select new TestEntity
                            {
                                PartitionKey = pk,
                                RowKey = i.ToString("000"),
                                Code = i,
                                Text = dt.ToString("yyyyMMddHHmmss"),
                            }).ToList();

            await table.BatchInsertAsync(entities);
            Assert.Collection(
                entities,
                Enumerable.Repeat((Action<TestEntity>)(entity => Assert.NotNull(entity.ETag)), Count).ToArray());
          //  table.Rows.Where(row=>row.PartitionKey).
            var retrieved = await table.QueryAsync(
                from row in table.Rows
                where row.PartitionKey == pk
                select row).ToListAsync();
            Assert.NotNull(retrieved);
            Assert.Equal(entities, retrieved, TestEntityComparer.Instance);

            foreach (var item in retrieved)
            {
                item.Code++;
            }
            await table.BatchUpdateAsync(retrieved);
            for (int i = 0; i < entities.Count; i++)
            {
                Assert.NotEqual(entities[i].ETag, retrieved[i].ETag);
            }

            var retrieved2 = await table.QueryAsync(
                from row in table.Rows
                where row.PartitionKey == pk
                select row).ToListAsync();
            Assert.NotNull(retrieved2);
            Assert.Equal(retrieved, retrieved2, TestEntityComparer.Instance);

            await Assert.ThrowsAsync<StorageException>(() => table.BatchDeleteAsync(entities));

            await table.BatchDeleteAsync(retrieved);
            Assert.Empty(await table.QueryAsync(
                from row in table.Rows
                where row.PartitionKey == pk
                select row).ToListAsync());
        }

        [SkippableFact]
        public async Task TestQuery()
        {
            var storage = new PerfStorage(Requirements.RequireStorage());
            var table = await storage.GetTableAsync<TestEntity>(nameof(TestQuery).ToLower());
            const int Count = 30;
            var dt = DateTime.Now;
            var pk = dt.ToString("yyyyMMddHHmmss");
            var entities = (from i in Enumerable.Range(0, Count)
                            select new TestEntity
                            {
                                PartitionKey = pk,
                                RowKey = i.ToString("000"),
                                Code = i,
                                Text = dt.ToString("yyyyMMddHHmmss"),
                            }).ToList();
            await table.BatchInsertAsync(entities);
            Assert.Collection(
                entities,
                Enumerable.Repeat((Action<TestEntity>)(entity => Assert.NotNull(entity.ETag)), Count).ToArray());

            var list1 = await table.QueryAsync(
                from row in table.Rows
                where row.PartitionKey == pk
                where row.RowKey > (ComparableString)"000" && row.RowKey < (ComparableString)"010"
                select row).ToListAsync();
            Assert.Equal(entities.Skip(1).Take(9), list1, TestEntityComparer.Instance);

            var list2 = await table.QueryAsync(
                from row in table.Rows
                where row.PartitionKey == pk
                where row.RowKey < (ComparableString)"003" || row.Code >= Count - 3
                select row).ToListAsync();
            Assert.Equal(entities.Take(3).Concat(entities.TakeLast(3)), list2, TestEntityComparer.Instance);

            var random = new Random();
            var codeToFind = random.Next(Count);
            var actual = await table.GetFirstOrDefaultAsync(
                from row in table.Rows
                where row.PartitionKey == pk && row.Code == codeToFind
                select row);
            Assert.NotNull(actual);
            Assert.Equal(entities.ElementAt(codeToFind), actual, TestEntityComparer.Instance);

            await table.BatchDeleteAsync(entities);
        }

        public class TestEntity : TableEntity
        {
            public string Text { get; set; }
            public long Code { get; set; }
        }

        public class TestEntityComparer : EqualityComparer<TestEntity>
        {
            public static readonly TestEntityComparer Instance = new TestEntityComparer();

            public override bool Equals([AllowNull] TestEntity x, [AllowNull] TestEntity y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }
                return x.PartitionKey == y.PartitionKey &&
                    x.RowKey == y.RowKey &&
                    x.Code == y.Code &&
                    x.Text == y.Text &&
                    x.ETag == y.ETag;
            }

            public override int GetHashCode([DisallowNull] TestEntity obj) =>
                HashCode.Combine(obj.PartitionKey, obj.RowKey, obj.Code, obj.Text, obj.ETag);
        }
    }
}
