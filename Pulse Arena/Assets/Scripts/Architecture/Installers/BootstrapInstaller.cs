using Architecture.States;
using Architecture.States.Interfaces;
using Zenject;

namespace Architecture.Installers
{
    public class BootstrapInstaller : MonoInstaller, IInitializable
    {
        public override void InstallBindings()
        {
            BindMyself();
            BindStateMachine();
            BindStates();
        }

        public void Initialize()
        {
            RegisterStates();
            Container.Resolve<IStateMachine>().Enter<BootstrapState>();
        }

        private void BindMyself()
        {
            Container
                .BindInterfacesTo<BootstrapInstaller>()
                .FromInstance(this)
                .AsSingle()
                .NonLazy();
        }

        private void BindStateMachine()
        {
            Container
                .Bind<IStateMachine>()
                .To<StateMachine>()
                .AsSingle();
        }

        private void BindStates()
        {
            Container.Bind<BootstrapState>().AsSingle();
            Container.Bind<LoadMainMenuState>().AsSingle();
            Container.Bind<StartGameState>().AsSingle();
        }

        private void RegisterStates()
        {
            IStateMachine stateMachine = Container.Resolve<IStateMachine>();

            stateMachine.AddState(Container.Resolve<BootstrapState>());
            stateMachine.AddState(Container.Resolve<LoadMainMenuState>());
            stateMachine.AddState(Container.Resolve<StartGameState>());
        }
    }
}
