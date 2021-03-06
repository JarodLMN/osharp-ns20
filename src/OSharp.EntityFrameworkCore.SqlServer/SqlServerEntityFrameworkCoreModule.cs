﻿// -----------------------------------------------------------------------
//  <copyright file="SqlServerEntityFrameworkCoreModule.cs" company="OSharp开源团队">
//      Copyright (c) 2014-2018 OSharp. All rights reserved.
//  </copyright>
//  <site>http://www.osharp.org</site>
//  <last-editor>郭明锋</last-editor>
//  <last-date>2018-03-07 21:53</last-date>
// -----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OSharp.Core;
using OSharp.Core.Modules;
using OSharp.Core.Options;


namespace OSharp.Entity.SqlServer
{
    /// <summary>
    /// SqlServerEntityFrameworkCore模块
    /// </summary>
    [DependsOnModules(typeof(EntityFrameworkCoreModule))]
    public class SqlServerEntityFrameworkCoreModule : OSharpModule
    {
        /// <summary>
        /// 获取 模块级别
        /// </summary>
        public override ModuleLevel Level => ModuleLevel.Framework;

        /// <summary>
        /// 获取 模块启动顺序，模块启动的顺序先按级别启动，级别内部再按此顺序启动
        /// </summary>
        public override int Order => 1;

        /// <summary>
        /// 将模块服务添加到依赖注入服务容器中
        /// </summary>
        /// <param name="services">依赖注入服务容器</param>
        /// <returns></returns>
        public override IServiceCollection AddServices(IServiceCollection services)
        {
            services.AddSingleton<IDbContextOptionsBuilderCreator, DbContextOptionsBuilderCreator>();
            return services;
        }
    }
}