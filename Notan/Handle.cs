namespace Notan
{
    //TODO: Make this a generic when https://github.com/dotnet/runtime/issues/6924 is finally fixed.
    public readonly struct Handle
    {
        public readonly int Index;
        public readonly int Generation;

        internal Handle(int index, int generation)
        {
            Index = index;
            Generation = generation;
        }
    }
}
