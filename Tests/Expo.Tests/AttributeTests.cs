using System.Reflection;
using System.Reflection.Emit;

namespace Brigade.Net.Expo.Tests;

public class AttributeTests
{
    public static TheoryData<Type> ValueComparisonTypes => new()
    {
        typeof(IsGreaterThanAttribute),
        typeof(IsGreaterThanOrEqualToAttribute),
        typeof(IsLessThanAttribute),
        typeof(IsLessThanOrEqualToAttribute),
        typeof(IsEqualToAttribute),
        typeof(IsNotEqualToAttribute)
    };

    public static TheoryData<Type> PropertyComparisonTypes => new()
    {
        typeof(IsGreaterThanXAttribute),
        typeof(IsGreaterThanOrEqualToXAttribute),
        typeof(IsLessThanXAttribute),
        typeof(IsLessThanOrEqualToXAttribute),
        typeof(IsEqualToXAttribute),
        typeof(IsNotEqualToXAttribute)
    };

    [Theory]
    [MemberData(nameof(ValueComparisonTypes))]
    public void ValueComparisonAttributesExposeValueAndMessage(Type attributeType)
    {
        var attribute = Assert.IsAssignableFrom<ValueComparisonAttribute>(
            Activator.CreateInstance(attributeType, 42, "bad value")
        );

        Assert.Equal(42, attribute.Value);
        Assert.Equal("bad value", attribute.Message);
        Assert.True(attributeType.IsSealed);
    }

    [Theory]
    [MemberData(nameof(PropertyComparisonTypes))]
    public void PropertyComparisonAttributesExposePropertyAndMessage(Type attributeType)
    {
        Assert.True(attributeType.IsAbstract);
        Assert.True(attributeType.IsSubclassOf(typeof(PropertyComparisonAttribute)));
        var attribute = Assert.IsAssignableFrom<PropertyComparisonAttribute>(
            CreateConcreteAttribute(attributeType, "Other", "bad relation")
        );

        Assert.Equal("Other", attribute.PropertyName);
        Assert.Equal("bad relation", attribute.Message);
    }

    [Fact]
    public void RequiredAttributeExposesMessage()
    {
        var attribute = new IsRequiredAttribute("required");

        Assert.Equal("required", attribute.Message);
    }

    [Fact]
    public void MarkerAttributesHaveExpectedTargets()
    {
        Assert.Equal(
            AttributeTargets.Class | AttributeTargets.Struct,
            Usage<ExpoAttribute>().ValidOn
        );
        Assert.Equal(AttributeTargets.Property, Usage<IsComparableAttribute>().ValidOn);
        Assert.Equal(
            AttributeTargets.Property | AttributeTargets.Method,
            Usage<CustomValidationAttribute>().ValidOn
        );
        Assert.Equal("Value", new CustomValidationAttribute("Value").PropertyName);
    }

    private static AttributeUsageAttribute Usage<TAttribute>()
        where TAttribute : Attribute
    {
        return typeof(TAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
    }

    private static Attribute CreateConcreteAttribute(
        Type baseType,
        string propertyName,
        string message
    )
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("ExpoAttributeTests" + baseType.Name),
            AssemblyBuilderAccess.Run
        );
        var type = assembly.DefineDynamicModule("Tests").DefineType(
            "Concrete" + baseType.Name,
            TypeAttributes.Class | TypeAttributes.Sealed,
            baseType
        );
        var baseConstructor = baseType.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(string)],
            null
        )!;
        var constructor = type.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(string), typeof(string)]
        );
        var body = constructor.GetILGenerator();
        body.Emit(OpCodes.Ldarg_0);
        body.Emit(OpCodes.Ldarg_1);
        body.Emit(OpCodes.Ldarg_2);
        body.Emit(OpCodes.Call, baseConstructor);
        body.Emit(OpCodes.Ret);
        var concreteType = type.CreateType()!;
        return (Attribute)Activator.CreateInstance(concreteType, propertyName, message)!;
    }
}
