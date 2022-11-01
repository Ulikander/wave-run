using UnityEngine;
using WASD.Interfaces;
using Zenject;

namespace WASD.Runtime.Managers
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<GameManager>().FromInstance(instance: Utils.GetGlobalInstance<GameManager>()).AsSingle().NonLazy();
            Container.Bind<InputManager>().FromInstance(instance: Utils.GetGlobalInstance<InputManager>()).AsSingle().NonLazy();
            Container.Bind<AudioManager>().FromInstance(instance: Utils.GetGlobalInstance<AudioManager>()).AsSingle().NonLazy();
            Container.Bind<ScenesManager>().FromInstance(instance: Utils.GetGlobalInstance<ScenesManager>()).AsSingle().NonLazy();
            Container.Bind<TaskManager>().FromInstance(instance: Utils.GetGlobalInstance<TaskManager>()).AsSingle().NonLazy();
        }
    }
}
