﻿// -----------------------------------------------------------------------
//  <copyright file="DistributedCacheExtensions.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2017 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor></last-editor>
//  <last-date>2017-09-17 16:45</last-date>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.Extensions.Caching.Distributed;

using OSharp.Collections;
using OSharp.Core.Functions;
using OSharp.Entity;
using OSharp.Filter;
using OSharp.Mapping;
using OSharp.Properties;


namespace OSharp.Caching
{
    /// <summary>
    /// <see cref="IDistributedCache"/>扩展方法
    /// </summary>
    public static class DistributedCacheExtensions
    {
        /// <summary>
        /// 将对象存入缓存中
        /// </summary>
        public static void Set(this IDistributedCache cache, string key, object value, DistributedCacheEntryOptions options = null)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));

            string json = value.ToJsonString();
            if (options == null)
            {
                cache.SetString(key, json);
            }
            else
            {
                cache.SetString(key, json, options);
            }
        }

        /// <summary>
        /// 将对象存入缓存中，使用指定时长
        /// </summary>
        public static void Set(this IDistributedCache cache, string key, object value, int cacheSeconds)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));
            Check.GreaterThan(cacheSeconds, nameof(cacheSeconds), 0, true);


            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            options.SetAbsoluteExpiration(TimeSpan.FromSeconds(cacheSeconds));
            cache.Set(key, value, options);
        }

        /// <summary>
        /// 将对象存入缓存中，使用功能配置
        /// </summary>
        public static void Set(this IDistributedCache cache, string key, object value, IFunction function)
        {
            Check.NotNullOrEmpty(key, nameof(key));
            Check.NotNull(value, nameof(value));
            Check.NotNull(function, nameof(function));

            if (function.CacheExpirationSeconds == 0)
            {
                return;
            }
            DistributedCacheEntryOptions options = function.ToCacheOptions();
            cache.Set(key, value, options);
        }

        /// <summary>
        /// 获取指定键的缓存项
        /// </summary>
        public static TResult Get<TResult>(this IDistributedCache cache, string key)
        {
            string json = cache.GetString(key);
            if (json == null)
            {
                return default(TResult);
            }
            return json.FromJsonString<TResult>();
        }

        /// <summary>
        /// 获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static TResult Get<TResult>(this IDistributedCache cache, string key, Func<TResult> getFunc, int cacheSeconds)
        {
            TResult result = cache.Get<TResult>(key);
            if (result != null)
            {
                return result;
            }
            result = getFunc();
            cache.Set(key, result, cacheSeconds);
            return result;
        }

        /// <summary>
        /// 获取指定键的缓存项，不存在则从指定委托获取，并回存到缓存中再返回
        /// </summary>
        public static TResult Get<TResult>(this IDistributedCache cache, string key, Func<TResult> getFunc, IFunction function)
        {
            TResult result = cache.Get<TResult>(key);
            if (result != null)
            {
                return result;
            }
            result = getFunc();
            cache.Set(key, result, function);
            return result;
        }

        /// <summary>
        /// 查询分页数据结果，如缓存存在，直接返回，否则从数据源查找分页结果，并存入缓存中再返回
        /// </summary>
        public static PageResult<TResult> ToPageCache<TEntity, TResult>(this IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> pridicate,
            PageCondition pageCondition,
            Expression<Func<TEntity, TResult>> selector,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source, pridicate, pageCondition, selector, keyParams);
            return cache.Get(key, () => source.ToPage(pridicate, pageCondition, selector), cacheSeconds);
        }

        /// <summary>
        /// 查询分页数据结果，如缓存存在，直接返回，否则从数据源查找分页结果，并存入缓存中再返回
        /// </summary>
        public static PageResult<TResult> ToPageCache<TEntity, TResult>(this IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> pridicate,
            PageCondition pageCondition,
            Expression<Func<TEntity, TResult>> selector,
            IFunction function,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source, pridicate, pageCondition, selector, keyParams);
            return cache.Get(key, () => source.ToPage(pridicate, pageCondition, selector), function);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="cacheSeconds">缓存时间：秒</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static List<TResult> ToCacheList<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            Expression<Func<TSource, TResult>> selector,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheList(selector, cacheSeconds, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="cacheSeconds">缓存时间：秒</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static TResult[] ToCacheArray<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            Expression<Func<TSource, TResult>> selector,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheArray(selector, cacheSeconds, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static List<TResult> ToCacheList<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            Expression<Func<TSource, TResult>> selector,
            IFunction function,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheList(selector, function, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static TResult[] ToCacheArray<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            Expression<Func<TSource, TResult>> selector,
            IFunction function,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheArray(selector, function, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static List<TResult> ToCacheList<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source, selector, keyParams);
            return cache.Get(key, () => source.Select(selector).ToList(), cacheSeconds);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static TResult[] ToCacheArray<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source, selector, keyParams);
            return cache.Get(key, () => source.Select(selector).ToArray(), cacheSeconds);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并按指定缓存策略存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static List<TResult> ToCacheList<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            IFunction function,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source, selector, keyParams);
            return cache.Get(key, () => source.Select(selector).ToList(), function);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并按指定缓存策略存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TResult">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="selector">数据筛选表达式</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static TResult[] ToCacheArray<TSource, TResult>(this IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            IFunction function,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source, selector, keyParams);
            return cache.Get(key, () => source.Select(selector).ToArray(), function);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static List<TSource> ToCacheList<TSource>(this IQueryable<TSource> source, int cacheSeconds = 60, params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source.Expression, keyParams);
            return cache.Get(key, source.ToList, cacheSeconds);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static TSource[] ToCacheArray<TSource>(this IQueryable<TSource> source, int cacheSeconds = 60, params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source.Expression, keyParams);
            return cache.Get(key, source.ToArray, cacheSeconds);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并按指定缓存策略存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static List<TSource> ToCacheList<TSource>(this IQueryable<TSource> source, IFunction function, params object[] keyParams)
        {
            if (function == null || function.CacheExpirationSeconds <= 0)
            {
                return source.ToList();
            }
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source.Expression, keyParams);
            return cache.Get(key, source.ToList, function);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并按指定缓存策略存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static TSource[] ToCacheArray<TSource>(this IQueryable<TSource> source, IFunction function, params object[] keyParams)
        {
            if (function == null || function.CacheExpirationSeconds <= 0)
            {
                return source.ToArray();
            }
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey(source.Expression, keyParams);
            return cache.Get(key, source.ToArray, function);
        }

        #region OutputDto

        /// <summary>
        /// 查询分页数据结果，如缓存存在，直接返回，否则从数据源查找分页结果，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TOutputDto">分页数据类型</typeparam>
        /// <param name="source">要查询的数据集</param>
        /// <param name="predicate">查询条件谓语表达式</param>
        /// <param name="pageCondition">分页查询条件</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询的分页结果</returns>
        public static PageResult<TOutputDto> ToPageCache<TEntity, TOutputDto>(this IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate,
            PageCondition pageCondition,
            int cacheSeconds = 60, params object[] keyParams)
            where TOutputDto : IOutputDto
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey<TEntity, TOutputDto>(source, predicate, pageCondition, keyParams);
            return cache.Get(key, () => source.ToPage<TEntity, TOutputDto>(predicate, pageCondition), cacheSeconds);
        }

        /// <summary>
        /// 查询分页数据结果，如缓存存在，直接返回，否则从数据源查找分页结果，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <typeparam name="TOutputDto">分页数据类型</typeparam>
        /// <param name="source">要查询的数据集</param>
        /// <param name="predicate">查询条件谓语表达式</param>
        /// <param name="pageCondition">分页查询条件</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询的分页结果</returns>
        public static PageResult<TOutputDto> ToPageCache<TEntity, TOutputDto>(this IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate,
            PageCondition pageCondition,
            IFunction function, params object[] keyParams)
            where TOutputDto : IOutputDto
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey<TEntity, TOutputDto>(source, predicate, pageCondition, keyParams);
            return cache.Get(key, () => source.ToPage<TEntity, TOutputDto>(predicate, pageCondition), function);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="cacheSeconds">缓存时间：秒</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static List<TOutputDto> ToCacheList<TSource, TOutputDto>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheList<TSource, TOutputDto>(cacheSeconds, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="cacheSeconds">缓存时间：秒</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static TOutputDto[] ToCacheArray<TSource, TOutputDto>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheArray<TSource, TOutputDto>(cacheSeconds, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static List<TOutputDto> ToCacheList<TSource, TOutputDto>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            IFunction function,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheList<TSource, TOutputDto>(function, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">数据源的项数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">数据源</param>
        /// <param name="predicate">数据查询表达式</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns></returns>
        public static TOutputDto[] ToCacheArray<TSource, TOutputDto>(this IQueryable<TSource> source,
            Expression<Func<TSource, bool>> predicate,
            IFunction function,
            params object[] keyParams)
        {
            return source.Where(predicate).ToCacheArray<TSource, TOutputDto>(function, keyParams);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static List<TOutputDto> ToCacheList<TSource, TOutputDto>(this IQueryable<TSource> source,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey<TSource, TOutputDto>(source, keyParams);
            return cache.Get(key, () => source.ToOutput<TOutputDto>().ToList(), cacheSeconds);
        }

        /// <summary>
        /// 将结果转换为缓存的数组，如缓存存在，直接返回，否则从数据源查询，并存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="cacheSeconds">缓存的秒数</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static TOutputDto[] ToCacheArray<TSource, TOutputDto>(this IQueryable<TSource> source,
            int cacheSeconds = 60,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey<TSource, TOutputDto>(source, keyParams);
            return cache.Get(key, () => source.ToOutput<TOutputDto>().ToArray(), cacheSeconds);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并按指定缓存策略存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static List<TOutputDto> ToCacheList<TSource, TOutputDto>(this IQueryable<TSource> source,
            IFunction function,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey<TSource, TOutputDto>(source, keyParams);
            return cache.Get(key, () => source.ToOutput<TOutputDto>().ToList(), function);
        }

        /// <summary>
        /// 将结果转换为缓存的列表，如缓存存在，直接返回，否则从数据源查询，并按指定缓存策略存入缓存中再返回
        /// </summary>
        /// <typeparam name="TSource">源数据类型</typeparam>
        /// <typeparam name="TOutputDto">结果集的项数据类型</typeparam>
        /// <param name="source">查询数据源</param>
        /// <param name="function">缓存策略相关功能</param>
        /// <param name="keyParams">缓存键参数</param>
        /// <returns>查询结果</returns>
        public static TOutputDto[] ToCacheArray<TSource, TOutputDto>(this IQueryable<TSource> source,
            IFunction function,
            params object[] keyParams)
        {
            IDistributedCache cache = ServiceLocator.Instance.GetService<IDistributedCache>();
            string key = GetKey<TSource, TOutputDto>(source, keyParams);
            return cache.Get(key, () => source.ToOutput<TOutputDto>().ToArray(), function);
        }

        #endregion

        /// <summary>
        /// 将<see cref="IFunction"/>的缓存配置转换为<see cref="DistributedCacheEntryOptions"/>
        /// </summary>
        public static DistributedCacheEntryOptions ToCacheOptions(this IFunction function)
        {
            Check.NotNull(function, nameof(function));
            if (function.CacheExpirationSeconds == 0)
            {
                return null;
            }
            DistributedCacheEntryOptions options = new DistributedCacheEntryOptions();
            if (!function.IsCacheSliding)
            {
                options.SetAbsoluteExpiration(TimeSpan.FromSeconds(function.CacheExpirationSeconds));
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromSeconds(function.CacheExpirationSeconds));
            }
            return options;
        }

        private static string GetKey<TEntity, TResult>(IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate,
            PageCondition pageCondition,
            Expression<Func<TEntity, TResult>> selector, params object[] keyParams)
        {
            if (!typeof(TEntity).IsEntityType())
            {
                throw new InvalidOperationException(Resources.QueryCacheExtensions_TypeNotEntityType.FormatWith(typeof(TEntity).FullName));
            }

            source = source.Where(predicate);
            SortCondition[] sortConditions = pageCondition.SortConditions;
            if (sortConditions == null || sortConditions.Length == 0)
            {
                source = source.OrderBy("Id");
            }
            else
            {
                int count = 0;
                IOrderedQueryable<TEntity> orderSource = null;
                foreach (SortCondition sortCondition in sortConditions)
                {
                    orderSource = count == 0
                        ? CollectionPropertySorter<TEntity>.OrderBy(source, sortCondition.SortField, sortCondition.ListSortDirection)
                        : CollectionPropertySorter<TEntity>.ThenBy(orderSource, sortCondition.SortField, sortCondition.ListSortDirection);
                    count++;
                }
                source = orderSource;
            }
            int pageIndex = pageCondition.PageIndex, pageSize = pageCondition.PageSize;
            source = source != null
                ? source.Skip((pageIndex - 1) * pageSize).Take(pageSize)
                : Enumerable.Empty<TEntity>().AsQueryable();
            IQueryable<TResult> query = source.Select(selector);
            return GetKey(query.Expression, keyParams);
        }

        private static string GetKey<TEntity, TOutputDto>(IQueryable<TEntity> source,
            Expression<Func<TEntity, bool>> predicate,
            PageCondition pageCondition,
            params object[] keyParams)
            where TOutputDto : IOutputDto
        {
            if (!typeof(TEntity).IsEntityType())
            {
                throw new InvalidOperationException(Resources.QueryCacheExtensions_TypeNotEntityType.FormatWith(typeof(TEntity).FullName));
            }

            source = source.Where(predicate);
            SortCondition[] sortConditions = pageCondition.SortConditions;
            if (sortConditions == null || sortConditions.Length == 0)
            {
                source = source.OrderBy("Id");
            }
            else
            {
                int count = 0;
                IOrderedQueryable<TEntity> orderSource = null;
                foreach (SortCondition sortCondition in sortConditions)
                {
                    orderSource = count == 0
                        ? CollectionPropertySorter<TEntity>.OrderBy(source, sortCondition.SortField, sortCondition.ListSortDirection)
                        : CollectionPropertySorter<TEntity>.ThenBy(orderSource, sortCondition.SortField, sortCondition.ListSortDirection);
                    count++;
                }
                source = orderSource;
            }
            int pageIndex = pageCondition.PageIndex, pageSize = pageCondition.PageSize;
            source = source != null
                ? source.Skip((pageIndex - 1) * pageSize).Take(pageSize)
                : Enumerable.Empty<TEntity>().AsQueryable();
            IQueryable<TOutputDto> query = source.ToOutput<TOutputDto>();
            return GetKey(query.Expression, keyParams);
        }

        private static string GetKey<TSource, TOutputDto>(IQueryable<TSource> source,
            params object[] keyParams)
        {
            IQueryable<TOutputDto> query = source.ToOutput<TOutputDto>();
            return GetKey(query.Expression, keyParams);
        }

        private static string GetKey<TSource, TResult>(IQueryable<TSource> source,
            Expression<Func<TSource, TResult>> selector,
            params object[] keyParams)
        {
            IQueryable<TResult> query = source.Select(selector);
            return GetKey(query.Expression, keyParams);
        }

        private static string GetKey(Expression expression, params object[] keyParams)
        {
            string key;
            try
            {
                key = new ExpressionCacheKeyGenerator(expression).GetKey(keyParams);
            }
            catch (TargetInvocationException)
            {
                key = new StringCacheKeyGenerator().GetKey(keyParams);
            }
            return key.ToMd5Hash();
        }
    }
}