using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using TwinCatQA.Domain.Models;
using TwinCatQA.Domain.Services;

namespace TwinCatQA.Infrastructure.Analysis;

/// <summary>
/// TwinCAT XML 기반 I/O 매핑 검증기
/// </summary>
public class IOMappingValidator : IIOMappingValidator
{
    private readonly ILogger<IOMappingValidator> _logger;

    public IOMappingValidator(ILogger<IOMappingValidator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IOMappingValidationResult> ValidateIOMappingAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("I/O 매핑 검증 시작: {ProjectPath}", projectPath);

        try
        {
            var devices = await GetIODevicesAsync(projectPath, cancellationToken);
            var etherCatMaster = await ValidateEtherCATConfigurationAsync(projectPath, cancellationToken);
            var unusedMappings = await FindUnusedIOMappingsAsync(projectPath, cancellationToken);
            var conflicts = await CheckIOMappingConflictsAsync(projectPath, cancellationToken);
            var cycleWarnings = await ValidateCycleTimeAsync(projectPath, 4000, cancellationToken);

            var result = new IOMappingValidationResult
            {
                IsValid = conflicts.Count == 0,
                Errors = conflicts,
                Warnings = cycleWarnings,
                Devices = devices,
                EtherCATMaster = etherCatMaster,
                ProjectPath = projectPath,
                ValidationTime = DateTime.UtcNow
            };

            _logger.LogInformation("I/O 매핑 검증 완료: 디바이스 {DeviceCount}개, 오류 {ErrorCount}개, 경고 {WarningCount}개",
                devices.Count, conflicts.Count, cycleWarnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "I/O 매핑 검증 중 오류 발생: {ProjectPath}", projectPath);

            return new IOMappingValidationResult
            {
                IsValid = false,
                ProjectPath = projectPath,
                ValidationTime = DateTime.UtcNow,
                Errors = new List<IOMappingError>
                {
                    new IOMappingError
                    {
                        ErrorType = IOMappingErrorType.DeviceNotConnected,
                        Message = $"I/O 구성 파일 읽기 실패: {ex.Message}",
                        Severity = IssueSeverity.Error
                    }
                }
            };
        }
    }

    /// <inheritdoc/>
    public async Task<List<IODevice>> GetIODevicesAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("I/O 디바이스 목록 조회: {ProjectPath}", projectPath);

        return await Task.Run(() =>
        {
            var devices = new List<IODevice>();

            // TwinCAT I/O 구성 파일 찾기 (*.tciod, *.tciox 등)
            var ioConfigFiles = FindIOConfigurationFiles(projectPath);

            foreach (var configFile in ioConfigFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var devicesFromFile = ParseIODevicesFromXml(configFile);
                    devices.AddRange(devicesFromFile);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "I/O 구성 파일 파싱 실패: {FilePath}", configFile);
                }
            }

            _logger.LogDebug("I/O 디바이스 {Count}개 발견", devices.Count);
            return devices;

        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<EtherCATMaster?> ValidateEtherCATConfigurationAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("EtherCAT 설정 검증: {ProjectPath}", projectPath);

        return await Task.Run(() =>
        {
            // TwinCAT 프로젝트에서 EtherCAT 마스터 구성 찾기
            var etherCatFiles = Directory.GetFiles(projectPath, "*EtherCAT*.xml", SearchOption.AllDirectories);

            if (etherCatFiles.Length == 0)
            {
                _logger.LogDebug("EtherCAT 구성 파일을 찾을 수 없습니다");
                return null;
            }

            try
            {
                var master = ParseEtherCATMasterFromXml(etherCatFiles.First());
                _logger.LogDebug("EtherCAT 마스터 발견: {MasterName}, 사이클 타임: {CycleTime}us",
                    master.Name, master.CycleTimeMicroseconds);
                return master;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EtherCAT 구성 파싱 실패");
                return null;
            }

        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<IOMapping>> FindUnusedIOMappingsAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("사용되지 않는 I/O 매핑 탐지: {ProjectPath}", projectPath);

        return await Task.Run(() =>
        {
            var unusedMappings = new List<IOMapping>();

            // I/O 매핑 목록 조회
            var devices = GetIODevicesAsync(projectPath, cancellationToken).GetAwaiter().GetResult();

            foreach (var device in devices)
            {
                foreach (var mapping in device.Mappings)
                {
                    if (!mapping.IsUsed)
                    {
                        unusedMappings.Add(mapping);
                    }
                }
            }

            _logger.LogDebug("사용되지 않는 I/O 매핑 {Count}개 발견", unusedMappings.Count);
            return unusedMappings;

        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<IOMappingError>> CheckIOMappingConflictsAsync(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("I/O 매핑 충돌 검사: {ProjectPath}", projectPath);

        return await Task.Run(() =>
        {
            var errors = new List<IOMappingError>();

            // I/O 주소 충돌 검사
            var devices = GetIODevicesAsync(projectPath, cancellationToken).GetAwaiter().GetResult();
            var addressMap = new Dictionary<string, List<IOMapping>>();

            foreach (var device in devices)
            {
                foreach (var mapping in device.Mappings)
                {
                    var addressKey = $"{mapping.Direction}_{mapping.BitOffset}";

                    if (!addressMap.ContainsKey(addressKey))
                    {
                        addressMap[addressKey] = new List<IOMapping>();
                    }
                    addressMap[addressKey].Add(mapping);
                }
            }

            // 중복 매핑 검사
            foreach (var kvp in addressMap)
            {
                if (kvp.Value.Count > 1)
                {
                    errors.Add(new IOMappingError
                    {
                        ErrorType = IOMappingErrorType.DuplicateMapping,
                        Message = $"중복된 I/O 주소 매핑: {kvp.Key}, 변수: {string.Join(", ", kvp.Value.Select(m => m.VariableName))}",
                        DeviceName = string.Join(", ", kvp.Value.Select(m => m.VariableName)),
                        Severity = IssueSeverity.Error
                    });
                }
            }

            _logger.LogDebug("I/O 매핑 충돌 {Count}개 발견", errors.Count);
            return errors;

        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<IOMappingWarning>> ValidateCycleTimeAsync(
        string projectPath,
        int recommendedCycleTimeMicroseconds = 4000,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("사이클 타임 검증: {ProjectPath}, 권장 사이클 타임: {CycleTime}us",
            projectPath, recommendedCycleTimeMicroseconds);

        return await Task.Run(() =>
        {
            var warnings = new List<IOMappingWarning>();

            var etherCatMaster = ValidateEtherCATConfigurationAsync(projectPath, cancellationToken).GetAwaiter().GetResult();

            if (etherCatMaster != null)
            {
                if (etherCatMaster.CycleTimeMicroseconds > recommendedCycleTimeMicroseconds)
                {
                    warnings.Add(new IOMappingWarning
                    {
                        WarningType = IOMappingWarningType.CycleTimeWarning,
                        Message = $"사이클 타임이 권장값({recommendedCycleTimeMicroseconds}us)보다 큽니다: {etherCatMaster.CycleTimeMicroseconds}us",
                        DeviceName = etherCatMaster.Name
                    });
                }

                if (etherCatMaster.CycleTimeMicroseconds < 1000)
                {
                    warnings.Add(new IOMappingWarning
                    {
                        WarningType = IOMappingWarningType.CycleTimeWarning,
                        Message = $"사이클 타임이 너무 짧습니다: {etherCatMaster.CycleTimeMicroseconds}us (최소 1000us 권장)",
                        DeviceName = etherCatMaster.Name
                    });
                }
            }

            _logger.LogDebug("사이클 타임 경고 {Count}개 발견", warnings.Count);
            return warnings;

        }, cancellationToken);
    }

    // ============================================
    // Private Helper Methods
    // ============================================

    private List<string> FindIOConfigurationFiles(string projectPath)
    {
        var ioConfigFiles = new List<string>();

        // TwinCAT I/O 구성 파일 확장자
        var extensions = new[] { "*.tciod", "*.tciox", "*_IO.xml" };

        foreach (var extension in extensions)
        {
            var files = Directory.GetFiles(projectPath, extension, SearchOption.AllDirectories);
            ioConfigFiles.AddRange(files);
        }

        return ioConfigFiles;
    }

    private List<IODevice> ParseIODevicesFromXml(string xmlFilePath)
    {
        var devices = new List<IODevice>();

        try
        {
            var xdoc = XDocument.Load(xmlFilePath);

            // TwinCAT XML 구조에 따라 디바이스 정보 추출
            // 실제 구조는 TwinCAT 버전에 따라 다를 수 있음
            var deviceElements = xdoc.Descendants("Device");

            foreach (var deviceElement in deviceElements)
            {
                var device = new IODevice
                {
                    Name = deviceElement.Attribute("Name")?.Value ?? "Unknown",
                    DeviceType = deviceElement.Attribute("Type")?.Value ?? "Unknown",
                    Vendor = deviceElement.Element("Info")?.Element("Vendor")?.Value ?? "Unknown",
                    ProductCode = deviceElement.Element("Info")?.Element("ProductCode")?.Value ?? "0",
                    Status = DeviceStatus.Unknown,
                    Mappings = new List<IOMapping>()
                };

                // I/O 매핑 정보 추출
                var mappings = ParseIOMappings(deviceElement);
                device.Mappings = mappings;
                device.InputCount = mappings.Count(m => m.Direction == IODirection.Input);
                device.OutputCount = mappings.Count(m => m.Direction == IODirection.Output);

                devices.Add(device);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "I/O 디바이스 XML 파싱 실패: {FilePath}", xmlFilePath);
        }

        return devices;
    }

    private List<IOMapping> ParseIOMappings(XElement deviceElement)
    {
        var mappings = new List<IOMapping>();

        // TwinCAT XML에서 I/O 매핑 정보 추출
        var linkElements = deviceElement.Descendants("Link");

        foreach (var linkElement in linkElements)
        {
            var mapping = new IOMapping
            {
                VariableName = linkElement.Attribute("VarName")?.Value ?? "Unknown",
                Direction = linkElement.Attribute("Direction")?.Value == "Input" ? IODirection.Input : IODirection.Output,
                DataType = linkElement.Attribute("Type")?.Value ?? "Unknown",
                BitOffset = int.Parse(linkElement.Attribute("Offset")?.Value ?? "0"),
                ByteSize = int.Parse(linkElement.Attribute("Size")?.Value ?? "0"),
                IsUsed = true // 실제 코드 분석을 통해 사용 여부 판단 필요
            };

            mappings.Add(mapping);
        }

        return mappings;
    }

    private EtherCATMaster ParseEtherCATMasterFromXml(string xmlFilePath)
    {
        var xdoc = XDocument.Load(xmlFilePath);

        // TwinCAT EtherCAT 마스터 정보 추출
        var masterElement = xdoc.Descendants("Master").FirstOrDefault();

        if (masterElement == null)
        {
            throw new InvalidOperationException("EtherCAT 마스터 정보를 찾을 수 없습니다");
        }

        var master = new EtherCATMaster
        {
            Name = masterElement.Attribute("Name")?.Value ?? "EtherCAT Master",
            CycleTimeMicroseconds = int.Parse(masterElement.Element("CycleTime")?.Value ?? "4000"),
            SlaveCount = masterElement.Descendants("Slave").Count(),
            UseDistributedClock = bool.Parse(masterElement.Element("DC")?.Attribute("Enabled")?.Value ?? "false"),
            CommunicationStatus = CommunicationStatus.Unknown
        };

        return master;
    }
}
