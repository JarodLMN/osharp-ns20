﻿// -----------------------------------------------------------------------
//  <copyright file="SqlServerMigrationModuleBase.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2018 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2018-03-20 16:57</last-date>
// -----------------------------------------------------------------------

using System;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using OSharp.Core.Modules;
using OSharp.Core.Options;
using OSharp.Exceptions;


namespace OSharp.Entity.SqlServer
{
    /// <summary>
    /// SqlServer数据迁移模块基类
    /// </summary>
    public abstract class SqlServerMigrationModuleBase<TDbContext> : OSharpModule
        where TDbContext : DbContext
    {
        /// <summary>
        /// 获取 模块级别
        /// </summary>
        public override ModuleLevel Level => ModuleLevel.Framework;

        /// <summary>
        /// 使用模块服务
        /// </summary>
        /// <param name="provider">服务提供者</param>
        public override void UseModule(IServiceProvider provider)
        {
            using (provider.GetService<IServiceScopeFactory>().CreateScope())
            {
                TDbContext context = CreateDbContext(provider);
                if (context != null)
                {
                    OSharpOptions options = provider.GetOSharpOptions();
                    OSharpDbContextOptions contextOptions = options.GetDbContextOptions(context.GetType());
                    if (contextOptions != null)
                    {
                        if (contextOptions.DatabaseType != DatabaseType.SqlServer)
                        {
                            throw new OsharpException($"上下文类型“{contextOptions.DatabaseType}”不是 SqlServer 类型");
                        }
                        if (contextOptions.AutoMigrationEnabled)
                        {
                            context.CheckAndMigration();
                        }
                    }
                }
            }

            IsEnabled = true;
        }

        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        protected abstract TDbContext CreateDbContext(IServiceProvider provider);
    }
}