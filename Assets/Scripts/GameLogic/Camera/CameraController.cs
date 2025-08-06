using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class CameraController : NetworkBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineVirtualCamera _virtualCamera;

    [Header("Rotate")]
    [SerializeField] private float _rotationSpeed = 100f;

    [Header("Zoom")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 20f;

    [Header("Focus")]
    [SerializeField] private float _focusTransitionTime = 0.5f;
    [SerializeField] private float _rotationSmoothness = 5f;

    private NetworkUnitSelectionSystem _selectionSystem;
    private Vector3 _baseOffset = new Vector3(0, 5f, -15f);
    private float _currentZoom;
    private float _currentRotationY;
    private bool _isFollowingUnit;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Vector3 _manualMovementOffset;

    private void Awake()
    {
        if (_virtualCamera == null)
        {
            _virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            if (_virtualCamera == null)
            {
                Debug.LogError("CinemachineVirtualCamera not found!");
                return;
            }

            _currentZoom = _baseOffset.magnitude;
        }
    }

    private void Start()
    {
        if (!IsOwner)
        {
            if (_virtualCamera != null)
            {
                _virtualCamera.gameObject.SetActive(false);
            }                
            return;
        }

        _selectionSystem = NetworkActionsSystem.instance?.UnitSelectionSystem;
        CenterOnSpawnZone();
        _currentRotationY = _virtualCamera.transform.eulerAngles.y;
        _targetRotation = Quaternion.Euler(0, _currentRotationY, 0);
        _targetPosition = _virtualCamera.transform.position;
    }

    private void CenterOnSpawnZone()
    {
        if (WorldGenerator.instance == null)
        {
            return;
        }            

        var spawnPoints = WorldGenerator.instance.GetTeamSpawnPoints();
        Player localTeam = NetworkManager.Singleton.IsHost ? Player.Player1 : Player.Player2;

        if (spawnPoints.TryGetValue(localTeam, out Vector3 spawnCenter))
        {
            Vector3 cameraPosition = spawnCenter + new Vector3(0, 3f, -10f);
            Vector3 lookAtPoint = spawnCenter - new Vector3(0, 2f, 0);

            _targetPosition = cameraPosition;
            _virtualCamera.transform.position = cameraPosition;
            _virtualCamera.transform.LookAt(lookAtPoint);

            // Обновляю базовые параметры
            _baseOffset = cameraPosition - spawnCenter;
            _currentZoom = _baseOffset.magnitude;
        }
    }


    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        UpdateCameraTarget();
        HandleUnitFocus();
        HandleRotation();
        HandleZoom();
        ApplyCameraMovement();
    }

    private void UpdateCameraTarget()
    {
        if (_selectionSystem?.SelectedUnit != null && _isFollowingUnit)
        {
            _targetPosition = _selectionSystem.SelectedUnit.transform.position + GetCurrentOffset() + _manualMovementOffset;
        }
    }

    private Vector3 GetCurrentOffset()
    {
        return Quaternion.Euler(0, _currentRotationY, 0) * (_baseOffset.normalized * _currentZoom);
    }

    private void HandleUnitFocus()
    {
        if (_selectionSystem?.SelectedUnit == null)
        {
            _isFollowingUnit = false;
            return;
        }

        // Начинаю слежение при выборе юнита
        if (!_isFollowingUnit)
        {
            _isFollowingUnit = true;
            _manualMovementOffset = Vector3.zero;
        }
    }

    private void HandleRotation()
    {
        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.A))
            rotationInput = -1f;
        if (Input.GetKey(KeyCode.D))
            rotationInput = 1f;

        if (rotationInput != 0f)
        {
            _currentRotationY += rotationInput * _rotationSpeed * Time.deltaTime;
            _targetRotation = Quaternion.Euler(0, _currentRotationY, 0);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.01f)
        {
            return;
        }

        _currentZoom = Mathf.Clamp(
            _currentZoom - scroll * _zoomSpeed,
            _minZoom,
            _maxZoom
        );
    }

    private void ApplyCameraMovement()
    {
        // Плавное перемещение
        _virtualCamera.transform.position = Vector3.Lerp(
            _virtualCamera.transform.position,
            _targetPosition,
            _focusTransitionTime * Time.deltaTime * 10f
        );

        // Плавное вращение
        _virtualCamera.transform.rotation = Quaternion.Slerp(
            _virtualCamera.transform.rotation,
            _targetRotation,
            _rotationSmoothness * Time.deltaTime
        );

        Vector3 lookAtTarget = _selectionSystem?.SelectedUnit != null && _isFollowingUnit
        ? _selectionSystem.SelectedUnit.transform.position
        : _virtualCamera.transform.position + _virtualCamera.transform.forward;

        Quaternion targetLookRotation = Quaternion.LookRotation(lookAtTarget - _virtualCamera.transform.position);
        _virtualCamera.transform.rotation = Quaternion.Slerp(
            _virtualCamera.transform.rotation,
            targetLookRotation,
            _rotationSmoothness * Time.deltaTime
        );

        // Смотрю в сторону выбранного юнита
        if (_selectionSystem?.SelectedUnit != null && _isFollowingUnit)
        {
            _virtualCamera.transform.LookAt(_selectionSystem.SelectedUnit.transform.position);
        }
        else
        {
            _virtualCamera.transform.LookAt(_virtualCamera.transform.position + _virtualCamera.transform.forward);
        }
    }
}
