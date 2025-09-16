using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class CameraController:MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _rotationSpeed = 100f;
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minZoom = 5f;
    [SerializeField] private float _maxZoom = 20f;
    [SerializeField] private float _focusTransitionTime = 0.5f;

    [Header("Cinemachine Settings")]
    [SerializeField] private Vector3 _baseOffset = new Vector3(0, 24f, -15f);

    private CinemachineVirtualCamera _virtualCamera;
    private NetworkUnitSelectionSystem _selectionSystem;
    private Transform _followTarget;

    private float _currentRotationY;
    private float _currentZoom;
    private bool _isFollowingUnit;
    private Vector3 _currentOffset;

    private void Awake()
    {
        _virtualCamera = GetComponent<CinemachineVirtualCamera>();
        _currentZoom = _baseOffset.magnitude;
        _currentOffset = _baseOffset;
    }

    public void Initialize(Transform playerObjectTransform)
    {
        _followTarget = playerObjectTransform;
        _selectionSystem = NetworkActionsSystem.instance.gameObject.GetComponent<NetworkUnitSelectionSystem>();

        CenterOnSpawnZone();
        _currentRotationY = transform.eulerAngles.y;

        _virtualCamera.Follow = _followTarget;
        _virtualCamera.LookAt = _followTarget;
        _virtualCamera.Priority = 100;
    }

    private void CenterOnSpawnZone()
    {
        Player localPlayer = NetworkPlayersManager.instance.GetLocalPlayer();
        Vector3 spawnCenter = localPlayer == Player.Player1
            ? WorldGenerator.instance.Team1SpawnCenter.Value
            : WorldGenerator.instance.Team2SpawnCenter.Value;

        Vector3 cameraPosition = spawnCenter + _baseOffset; // Используем _baseOffset напрямую
        transform.position = cameraPosition;
        transform.LookAt(spawnCenter);

        _currentZoom = Mathf.Abs(_baseOffset.z);
        _currentOffset = _baseOffset;
        _currentRotationY = transform.eulerAngles.y;
    }

    private void Update()
    {
        if (_followTarget == null)
            return;

        CheckIfUnitIsDestroyed();
        UpdateCameraTarget();
        HandleUnitFocus();
        HandleRotation();
        HandleZoom();
        ApplyManualCameraMovement();
    }

    private void CheckIfUnitIsDestroyed()
    {
        if (_isFollowingUnit && _selectionSystem?.SelectedUnit == null)
        {
            _isFollowingUnit = false;
            ReturnToDefaultTarget();
        }
    }

    private void ReturnToDefaultTarget()
    {
        _virtualCamera.Follow = _followTarget;
        _virtualCamera.LookAt = _followTarget;

        float currentHeight = _currentOffset.y;
        _currentOffset = new Vector3(0, currentHeight, -_baseOffset.magnitude);
        _currentZoom = _baseOffset.magnitude;

        Quaternion rotation = Quaternion.Euler(0, _currentRotationY, 0);
        _currentOffset = rotation * _currentOffset;
    }

    private void UpdateCameraTarget()
    {
        if (_selectionSystem?.SelectedUnit != null && _isFollowingUnit)
        {
            _virtualCamera.Follow = _selectionSystem.SelectedUnit.transform;
            _virtualCamera.LookAt = _selectionSystem.SelectedUnit.transform;
        }
    }

    private void HandleUnitFocus()
    {
        if (_selectionSystem?.SelectedUnit == null)
        {
            _isFollowingUnit = false;
            return;
        }

        if (!_isFollowingUnit)
        {
            _isFollowingUnit = true;
            _virtualCamera.Follow = _selectionSystem.SelectedUnit.transform;
            _virtualCamera.LookAt = _selectionSystem.SelectedUnit.transform;
        }
    }

    private void HandleRotation()
    {
        if (!_isFollowingUnit)
        {
            return;
        }

        float rotationInput = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            rotationInput = -1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            rotationInput = 1f;
        }

        if (rotationInput != 0f)
        {
            _currentRotationY += rotationInput * _rotationSpeed * Time.deltaTime;

            // Сохраняем текущую высоту перед поворотом
            float currentHeight = _currentOffset.y;

            // Обновляем смещение камеры с учетом поворота, но сохраняем высоту
            Quaternion rotation = Quaternion.Euler(0, _currentRotationY, 0);
            _currentOffset = rotation * new Vector3(0, currentHeight, -_currentZoom);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _currentZoom = Mathf.Clamp(
                _currentZoom - scroll * _zoomSpeed,
                _minZoom,
                _maxZoom
            );

            // Сохраняем текущую высоту перед зумом
            float currentHeight = _currentOffset.y;

            // Обновляем смещение с учетом нового зума, но сохраняем высоту
            Quaternion rotation = Quaternion.Euler(0, _currentRotationY, 0);
            _currentOffset = rotation * new Vector3(0, currentHeight, -_currentZoom);
        }
    }

    private void ApplyManualCameraMovement()
    {
        if (_virtualCamera.Follow == null)
        {
            return;
        }

        Vector3 targetPosition = _virtualCamera.Follow.position + _currentOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            _focusTransitionTime * Time.deltaTime * 10f
        );

        // Направляем камеру на цель
        if (_virtualCamera.LookAt != null)
        {
            transform.LookAt(_virtualCamera.LookAt.position);
        }
    }
}