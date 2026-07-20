using System.Collections;
using Architecture.Services.Interfaces;
using UnityEngine;

namespace Architecture.Services
{
    public class CoroutineRunner : MonoBehaviour, ICoroutineRunner
    {
        // MUST stay explicit interface implementations, not public. MonoBehaviour already defines public
        // StartCoroutine/StopCoroutine; explicit members let the inner call bind to the base method. Making these
        // public with the same signature would bind the inner call to itself → infinite recursion → editor crash.
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