﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DefaultEcs.System;

namespace DefaultEcs.Technical.System
{
    internal static class EntitySetBuilderFactory
    {
        #region Fields

        private static readonly ConcurrentDictionary<Type, Func<World, EntitySetBuilder>> _entitySetBuilderFactories;
        private static readonly MethodInfo _with;
        private static readonly MethodInfo _without;
        private static readonly MethodInfo _withAny;
        private static readonly MethodInfo _whenAdded;
        private static readonly MethodInfo _whenChanged;
        private static readonly MethodInfo _whenRemoved;

        #endregion

        #region Initialisation

        static EntitySetBuilderFactory()
        {
            _entitySetBuilderFactories = new ConcurrentDictionary<Type, Func<World, EntitySetBuilder>>();

            TypeInfo entitySetBuilder = typeof(EntitySetBuilder).GetTypeInfo();
            _with = entitySetBuilder.GetDeclaredMethods(nameof(EntitySetBuilder.With)).First(m => !m.ContainsGenericParameters);
            _without = entitySetBuilder.GetDeclaredMethods(nameof(EntitySetBuilder.Without)).First(m => !m.ContainsGenericParameters);
            _withAny = entitySetBuilder.GetDeclaredMethods(nameof(EntitySetBuilder.WithAny)).First(m => !m.ContainsGenericParameters);
            _whenAdded = entitySetBuilder.GetDeclaredMethods(nameof(EntitySetBuilder.WhenAdded)).First(m => !m.ContainsGenericParameters);
            _whenChanged = entitySetBuilder.GetDeclaredMethods(nameof(EntitySetBuilder.WhenChanged)).First(m => !m.ContainsGenericParameters);
            _whenRemoved = entitySetBuilder.GetDeclaredMethods(nameof(EntitySetBuilder.WhenRemoved)).First(m => !m.ContainsGenericParameters);
        }

        #endregion

        #region Methods

        private static Func<World, EntitySetBuilder> GetEntitySetBuilderFactory(Type type)
        {
            ParameterExpression world = Expression.Parameter(typeof(World));
            Expression expression = Expression.Call(world, typeof(World).GetTypeInfo().GetDeclaredMethod(nameof(World.GetEntities)));

            foreach (ComponentAttribute attribute in type.GetTypeInfo().GetCustomAttributes<ComponentAttribute>(true))
            {
                switch (attribute.FilterType)
                {
                    case ComponentFilterType.With:
                        expression = Expression.Call(expression, _with, Expression.Constant(attribute.ComponentTypes));
                        break;

                    case ComponentFilterType.Without:
                        expression = Expression.Call(expression, _without, Expression.Constant(attribute.ComponentTypes));
                        break;

                    case ComponentFilterType.WithAny:
                        expression = Expression.Call(expression, _withAny, Expression.Constant(attribute.ComponentTypes));
                        break;

                    case ComponentFilterType.WhenAdded:
                        expression = Expression.Call(expression, _whenAdded, Expression.Constant(attribute.ComponentTypes));
                        break;

                    case ComponentFilterType.WhenChanged:
                        expression = Expression.Call(expression, _whenChanged, Expression.Constant(attribute.ComponentTypes));
                        break;

                    case ComponentFilterType.WhenRemoved:
                        expression = Expression.Call(expression, _whenRemoved, Expression.Constant(attribute.ComponentTypes));
                        break;
                }
            }

            return Expression.Lambda<Func<World, EntitySetBuilder>>(expression, world).Compile();
        }

        public static Func<World, EntitySetBuilder> Create(Type type) => _entitySetBuilderFactories.GetOrAdd(type, GetEntitySetBuilderFactory);

        #endregion
    }
}