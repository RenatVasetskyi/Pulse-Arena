using System.Collections;
using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        // NOTE: these MUST be explicit interface implementations, NOT public methods.
        // MonoBehaviour already defines `public Coroutine StartCoroutine(IEnumerator)` /
        // `public void StopCoroutine(Coroutine)`. As explicit interface members these have a
        // different resolution scope, so `StartCoroutine(routine)` inside the body binds to the
        // BASE MonoBehaviour method. If you make them `public` with the same signature, that inner
        // call binds to ITSELF → infinite recursion → StackOverflow → hard editor crash on play.
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