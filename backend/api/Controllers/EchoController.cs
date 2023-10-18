﻿using System.Globalization;
using System.Text.Json;
using Api.Controllers.Models;
using Api.Services;
using Api.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Api.Controllers
{
    [ApiController]
    [Route("echo")]
    [Authorize(Roles = Role.Any)]
    public class EchoController : ControllerBase
    {

        private readonly IEchoService _echoService;
        private readonly ILogger<EchoController> _logger;

        private readonly IRobotService _robotService;

        public EchoController(ILogger<EchoController> logger, IEchoService echoService, IRobotService robotService)
        {
            _logger = logger;
            _echoService = echoService;
            _robotService = robotService;
        }

        /// <summary>
        ///     List all available Echo missions for the installation
        /// </summary>
        /// <remarks>
        ///     These missions are created in the Echo mission planner
        /// </remarks>
        [HttpGet]
        [Route("available-missions/{plantCode}")]
        [ProducesResponseType(typeof(List<CondensedEchoMissionDefinition>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<IList<CondensedEchoMissionDefinition>>> GetAvailableEchoMissions([FromRoute] string? plantCode)
        {
            try
            {
                var missions = await _echoService.GetAvailableMissions(plantCode);
                return Ok(missions);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error retrieving missions from Echo");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error retrieving missions from Echo");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        ///     Lookup Echo mission by Id
        /// </summary>
        /// <remarks>
        ///     This mission is created in the Echo mission planner
        /// </remarks>
        [HttpGet]
        [Route("missions/{missionId}")]
        [ProducesResponseType(typeof(EchoMission), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<EchoMission>> GetEchoMission([FromRoute] int missionId)
        {
            try
            {
                var mission = await _echoService.GetMissionById(missionId);
                return Ok(mission);
            }
            catch (HttpRequestException e)
            {
                if (e.StatusCode.HasValue && (int)e.StatusCode.Value == 404)
                {
                    _logger.LogWarning("Could not find echo mission with id={id}", missionId);
                    return NotFound("Echo mission not found");
                }

                _logger.LogError(e, "Error getting mission from Echo");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing mission from Echo");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            catch (InvalidDataException e)
            {
                string message =
                    "EchoMission invalid: One or more tags are missing associated robot poses.";
                _logger.LogError(e, message);
                return StatusCode(StatusCodes.Status502BadGateway, message);
            }
        }

        /// <summary>
        ///     Get selected information on all the plants in Echo
        /// </summary>
        [HttpGet]
        [Route("plants")]
        [ProducesResponseType(typeof(List<EchoPlantInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<EchoPlantInfo>> GetEchoPlantInfos()
        {
            try
            {
                var echoPlantInfos = await _echoService.GetEchoPlantInfos();
                return Ok(echoPlantInfos);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error getting plant info from Echo");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing plant info response from Echo");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        ///     Get all plants associated with an active robot.
        /// </summary>
        /// <remarks>
        ///     <para> Retrieves the plants that have an active robot </para>
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = Role.User)]
        [Route("active-plants")]
        [ProducesResponseType(typeof(IList<EchoPlantInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IList<EchoPlantInfo>>> GetActivePlants()
        {
            var plants = await _robotService.ReadAllActivePlants();

            if (plants == null)
            {
                _logger.LogWarning("Could not retrieve robot plants information");
                throw new RobotInformationNotAvailableException("Could not retrieve robot plants information");
            }

            plants = plants.Select(p => p.ToLower(CultureInfo.CurrentCulture));

            try
            {
                var echoPlantInfos = await _echoService.GetEchoPlantInfos();

                echoPlantInfos = echoPlantInfos.Where(p => plants.Contains(p.PlantCode.ToLower(CultureInfo.CurrentCulture))).ToList();
                return Ok(echoPlantInfos);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error getting plant info from Echo");
                return new StatusCodeResult(StatusCodes.Status502BadGateway);
            }
            catch (JsonException e)
            {
                _logger.LogError(e, "Error deserializing plant info response from Echo");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
