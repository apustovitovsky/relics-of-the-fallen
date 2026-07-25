using GAS;

namespace RelicsOfTheFallen.AbilitySystem
{
    public interface IAttributeReconciler
    {
        bool TryReconcile(
            AbilitySystemComponent target,
            string rejectedActivationId,
            AttributeSnapshot snapshot);
    }
}