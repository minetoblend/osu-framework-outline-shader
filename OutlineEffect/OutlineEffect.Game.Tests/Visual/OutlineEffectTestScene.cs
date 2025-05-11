using osu.Framework.Testing;

namespace OutlineEffect.Game.Tests.Visual
{
    public abstract partial class OutlineEffectTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new OutlineEffectTestSceneTestRunner();

        private partial class OutlineEffectTestSceneTestRunner : OutlineEffectGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}