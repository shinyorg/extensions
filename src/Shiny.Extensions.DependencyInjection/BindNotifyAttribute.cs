namespace Shiny;


/// <summary>
/// Opts a partial class with <see cref="BindAttribute"/> properties into a generated
/// <see cref="System.ComponentModel.INotifyPropertyChanged"/> implementation. Each bind setter
/// raises <c>PropertyChanged</c> after the store write, and only when the new value differs
/// from the value already in the store.
/// Ignored when the class already implements <see cref="System.ComponentModel.INotifyPropertyChanged"/>;
/// in that case use the generated <c>partial void On{Name}Changed(T oldValue, T newValue)</c> hook
/// to raise notifications through your own implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class BindNotifyAttribute : Attribute { }
