using System.Collections;
using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        Coroutine ICoroutineRunner.StartCoroutine(IEnumerator routine)
        {
            if (this == null || routine == null)
                return null;

            return StartCoroutine(routine);
        }

        void ICoroutineRunner.StopCoroutine(Coroutine routine)
        {
            if (this == null || routine == null)
                return;

            StopCoroutine(routine);
        }
    }
}
