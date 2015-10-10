using System;

namespace CachePerfExperiment
{
    /// <summary>
    /// Interface defining setup of a decorator pattern.
    /// </summary>
    /// <typeparam name="TComponent">Type of thing being decorated</typeparam>
    interface IDecorator<TComponent>
    {
        TComponent Next { get; }
        void Wrap(TComponent inner);
    }

    class Decorator
    {
        public static TComponent Chain<TComponent>(params TComponent[] components)
        {
            if (components.Length == 1)
            {
                return components[0];
            }
            for (int i = components.Length - 2; i >= 0; --i)
            {
                var decorator = components[i] as IDecorator<TComponent>;
                if (decorator == null)
                {
                    throw new ArgumentException(string.Format("Object at index {0} is not a decorator", i), "components");
                }

                decorator.Wrap(components[i+1]);
            }
            return components[0];
        }
    }
}
