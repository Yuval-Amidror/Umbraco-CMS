﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.Models.ContentEditing;
using System.Text;
using Constants = Umbraco.Core.Constants;
using Umbraco.Core.Mapping;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Web.BackOffice.Filters;
using Umbraco.Web.Common.Attributes;
using Umbraco.Web.Common.Exceptions;
using Umbraco.Web.Editors;

namespace Umbraco.Web.BackOffice.Controllers
{
    /// <summary>
    /// The API controller used for editing data types
    /// </summary>
    /// <remarks>
    /// The security for this controller is defined to allow full CRUD access to data types if the user has access to either:
    /// Content Types, Member Types or Media Types ... and of course to Data Types
    /// </remarks>
    [PluginController(Constants.Web.Mvc.BackOfficeApiArea)]
    [UmbracoTreeAuthorizeAttribute(Constants.Trees.DataTypes, Constants.Trees.DocumentTypes, Constants.Trees.MediaTypes, Constants.Trees.MemberTypes)]
    public class DataTypeController : BackOfficeNotificationsController
    {
        private readonly PropertyEditorCollection _propertyEditors;
        private readonly IDataTypeService _dataTypeService;
        private readonly IContentSettings _contentSettings;
        private readonly UmbracoMapper _umbracoMapper;
        private readonly PropertyEditorCollection _propertyEditorCollection;
        private readonly IContentTypeService _contentTypeService;
        private readonly IMediaTypeService _mediaTypeService;
        private readonly IMemberTypeService _memberTypeService;
        private readonly ILocalizedTextService _localizedTextService;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public DataTypeController(
            PropertyEditorCollection propertyEditors,
            IDataTypeService dataTypeService,
            IContentSettings contentSettings,
            UmbracoMapper umbracoMapper,
            PropertyEditorCollection propertyEditorCollection,
            IContentTypeService contentTypeService,
            IMediaTypeService mediaTypeService,
            IMemberTypeService memberTypeService,
            ILocalizedTextService localizedTextService,
            IUmbracoContextAccessor umbracoContextAccessor)
         {
            _propertyEditors = propertyEditors ?? throw new ArgumentNullException(nameof(propertyEditors));
            _dataTypeService = dataTypeService ?? throw new ArgumentNullException(nameof(dataTypeService));
            _contentSettings = contentSettings ?? throw new ArgumentNullException(nameof(contentSettings));
            _umbracoMapper = umbracoMapper ?? throw new ArgumentNullException(nameof(umbracoMapper));
            _propertyEditorCollection = propertyEditorCollection ?? throw new ArgumentNullException(nameof(propertyEditorCollection));
            _contentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
            _mediaTypeService = mediaTypeService ?? throw new ArgumentNullException(nameof(mediaTypeService));
            _memberTypeService = memberTypeService ?? throw new ArgumentNullException(nameof(memberTypeService));
            _localizedTextService = localizedTextService ?? throw new ArgumentNullException(nameof(localizedTextService));
            _umbracoContextAccessor = umbracoContextAccessor ?? throw new ArgumentNullException(nameof(umbracoContextAccessor));
         }

        /// <summary>
        /// Gets data type by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DataTypeDisplay GetByName(string name)
        {
            var dataType = _dataTypeService.GetDataType(name);
            return dataType == null ? null : _umbracoMapper.Map<IDataType, DataTypeDisplay>(dataType);
        }

        /// <summary>
        /// Gets the datatype json for the datatype id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DetermineAmbiguousActionByPassingParameters]
        public DataTypeDisplay GetById(int id)
        {
            var dataType = _dataTypeService.GetDataType(id);
            if (dataType == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return _umbracoMapper.Map<IDataType, DataTypeDisplay>(dataType);
        }

        /// <summary>
        /// Gets the datatype json for the datatype guid
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DetermineAmbiguousActionByPassingParameters]
        public DataTypeDisplay GetById(Guid id)
        {
            var dataType = _dataTypeService.GetDataType(id);
            if (dataType == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return _umbracoMapper.Map<IDataType, DataTypeDisplay>(dataType);
        }

        /// <summary>
        /// Gets the datatype json for the datatype udi
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [DetermineAmbiguousActionByPassingParameters]
        public DataTypeDisplay GetById(Udi id)
        {
            var guidUdi = id as GuidUdi;
            if (guidUdi == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            var dataType = _dataTypeService.GetDataType(guidUdi.Guid);
            if (dataType == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return _umbracoMapper.Map<IDataType, DataTypeDisplay>(dataType);
        }

        /// <summary>
        /// Deletes a data type with a given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [HttpPost]
        public IActionResult DeleteById(int id)
        {
            var foundType = _dataTypeService.GetDataType(id);
            if (foundType == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            var currentUser = _umbracoContextAccessor.GetRequiredUmbracoContext().Security.CurrentUser;
            _dataTypeService.Delete(foundType, currentUser.Id);

            return Ok();
        }

        public DataTypeDisplay GetEmpty(int parentId)
        {
            // cannot create an "empty" data type, so use something by default.
            var editor = _propertyEditors[Constants.PropertyEditors.Aliases.Label];
            var dt = new DataType(editor, parentId);
            return _umbracoMapper.Map<IDataType, DataTypeDisplay>(dt);
        }

        /// <summary>
        /// Returns a custom listview, based on a content type alias, if found
        /// </summary>
        /// <param name="contentTypeAlias"></param>
        /// <returns>a DataTypeDisplay</returns>
        public DataTypeDisplay GetCustomListView(string contentTypeAlias)
        {
            var dt = _dataTypeService.GetDataType(Constants.Conventions.DataTypes.ListViewPrefix + contentTypeAlias);
            if (dt == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return _umbracoMapper.Map<IDataType, DataTypeDisplay>(dt);
        }

        /// <summary>
        /// Creates a custom list view - give a document type alias
        /// </summary>
        /// <param name="contentTypeAlias"></param>
        /// <returns></returns>
        public DataTypeDisplay PostCreateCustomListView(string contentTypeAlias)
        {
            var dt = _dataTypeService.GetDataType(Constants.Conventions.DataTypes.ListViewPrefix + contentTypeAlias);

            //if it doesn't exist yet, we will create it.
            if (dt == null)
            {
                var editor = _propertyEditors[Constants.PropertyEditors.Aliases.ListView];
                dt = new DataType(editor) { Name = Constants.Conventions.DataTypes.ListViewPrefix + contentTypeAlias };
                _dataTypeService.Save(dt);
            }

            return _umbracoMapper.Map<IDataType, DataTypeDisplay>(dt);
        }

        /// <summary>
        /// Returns the pre-values for the specified property editor
        /// </summary>
        /// <param name="editorAlias"></param>
        /// <param name="dataTypeId">The data type id for the pre-values, -1 if it is a new data type</param>
        /// <returns></returns>
        public IEnumerable<DataTypeConfigurationFieldDisplay> GetPreValues(string editorAlias, int dataTypeId = -1)
        {
            var propEd = _propertyEditors[editorAlias];
            if (propEd == null)
            {
                throw new InvalidOperationException("Could not find property editor with alias " + editorAlias);
            }

            if (dataTypeId == -1)
            {
                //this is a new data type, so just return the field editors with default values
                return _umbracoMapper.Map<IDataEditor, IEnumerable<DataTypeConfigurationFieldDisplay>>(propEd);
            }

            //we have a data type associated
            var dataType = _dataTypeService.GetDataType(dataTypeId);
            if (dataType == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            //now, lets check if the data type has the current editor selected, if that is true
            //we will need to wire up it's saved values. Otherwise it's an existing data type
            //that is changing it's underlying property editor, in which case there's no values.
            if (dataType.EditorAlias == editorAlias)
            {
                //this is the currently assigned pre-value editor, return with values.
                return _umbracoMapper.Map<IDataType, IEnumerable<DataTypeConfigurationFieldDisplay>>(dataType);
            }

            //these are new pre-values, so just return the field editors with default values
            return _umbracoMapper.Map<IDataEditor, IEnumerable<DataTypeConfigurationFieldDisplay>>(propEd);
        }

        /// <summary>
        /// Deletes a data type container with a given ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [HttpPost]
        public IActionResult DeleteContainer(int id)
        {

            var currentUser = _umbracoContextAccessor.GetRequiredUmbracoContext().Security.CurrentUser;
            _dataTypeService.DeleteContainer(id, currentUser.Id);

            return Ok();
        }

        public IActionResult PostCreateContainer(int parentId, string name)
        {
            var currentUser = _umbracoContextAccessor.GetRequiredUmbracoContext().Security.CurrentUser;
            var result = _dataTypeService.CreateContainer(parentId, name, currentUser.Id);

            return result
                ? Ok(result.Result) //return the id
                : throw HttpResponseException.CreateNotificationValidationErrorResponse(result.Exception.Message);
        }

        /// <summary>
        /// Saves the data type
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        [DataTypeValidate]
        public ActionResult<DataTypeDisplay> PostSave(DataTypeSave dataType)
        {
            //If we've made it here, then everything has been wired up and validated by the attribute

            // TODO: Check if the property editor has changed, if it has ensure we don't pass the
            // existing values to the new property editor!

            // get the current configuration,
            // get the new configuration as a dictionary (this is how we get it from model)
            // and map to an actual configuration object
            var currentConfiguration = dataType.PersistedDataType.Configuration;
            var configurationDictionary = dataType.ConfigurationFields.ToDictionary(x => x.Key, x => x.Value);
            var configuration = dataType.PropertyEditor.GetConfigurationEditor().FromConfigurationEditor(configurationDictionary, currentConfiguration);

            dataType.PersistedDataType.Configuration = configuration;

            var currentUser = _umbracoContextAccessor.GetRequiredUmbracoContext().Security.CurrentUser;
            // save the data type
            try
            {

                _dataTypeService.Save(dataType.PersistedDataType, currentUser.Id);
            }
            catch (DuplicateNameException ex)
            {
                ModelState.AddModelError("Name", ex.Message);
                throw HttpResponseException.CreateValidationErrorResponse(ModelState);
            }

            // map back to display model, and return
            var display = _umbracoMapper.Map<IDataType, DataTypeDisplay>(dataType.PersistedDataType);
            display.AddSuccessNotification(_localizedTextService.Localize("speechBubbles/dataTypeSaved"), "");
            return display;
        }

        /// <summary>
        /// Move the media type
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public IActionResult PostMove(MoveOrCopy move)
        {
            var toMove = _dataTypeService.GetDataType(move.Id);
            if (toMove == null)
            {
                return NotFound();
            }

            var result = _dataTypeService.Move(toMove, move.ParentId);
            if (result.Success)
            {
                return Content(toMove.Path,"text/plain", Encoding.UTF8);
            }

            switch (result.Result.Result)
            {
                case MoveOperationStatusType.FailedParentNotFound:
                    return NotFound();
                case MoveOperationStatusType.FailedCancelledByEvent:
                    //returning an object of INotificationModel will ensure that any pending
                    // notification messages are added to the response.
                    throw HttpResponseException.CreateValidationErrorResponse(new SimpleNotificationModel());
                case MoveOperationStatusType.FailedNotAllowedByPath:
                    var notificationModel = new SimpleNotificationModel();
                    notificationModel.AddErrorNotification(_localizedTextService.Localize("moveOrCopy/notAllowedByPath"), "");
                    throw HttpResponseException.CreateValidationErrorResponse(notificationModel);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public IActionResult PostRenameContainer(int id, string name)
        {
            var currentUser = _umbracoContextAccessor.GetRequiredUmbracoContext().Security.CurrentUser;
            var result = _dataTypeService.RenameContainer(id, name, currentUser.Id);

            return result
                ? Ok(result.Result)
                : throw HttpResponseException.CreateNotificationValidationErrorResponse(result.Exception.Message);
        }

        /// <summary>
        /// Returns the references (usages) for the data type
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public DataTypeReferences GetReferences(int id)
        {
            var result = new DataTypeReferences();
            var usages = _dataTypeService.GetReferences(id);

            foreach(var groupOfEntityType in usages.GroupBy(x => x.Key.EntityType))
            {
                //get all the GUIDs for the content types to find
                var guidsAndPropertyAliases = groupOfEntityType.ToDictionary(i => ((GuidUdi)i.Key).Guid, i => i.Value);

                if (groupOfEntityType.Key == ObjectTypes.GetUdiType(UmbracoObjectTypes.DocumentType))
                    result.DocumentTypes = GetContentTypeUsages(_contentTypeService.GetAll(guidsAndPropertyAliases.Keys), guidsAndPropertyAliases);
                else if (groupOfEntityType.Key == ObjectTypes.GetUdiType(UmbracoObjectTypes.MediaType))
                    result.MediaTypes = GetContentTypeUsages(_mediaTypeService.GetAll(guidsAndPropertyAliases.Keys), guidsAndPropertyAliases);
                else if (groupOfEntityType.Key == ObjectTypes.GetUdiType(UmbracoObjectTypes.MemberType))
                    result.MemberTypes = GetContentTypeUsages(_memberTypeService.GetAll(guidsAndPropertyAliases.Keys), guidsAndPropertyAliases);
            }

            return result;
        }

        /// <summary>
        /// Maps the found content types and usages to the resulting model
        /// </summary>
        /// <param name="cts"></param>
        /// <param name="usages"></param>
        /// <returns></returns>
        private IEnumerable<DataTypeReferences.ContentTypeReferences> GetContentTypeUsages(
            IEnumerable<IContentTypeBase> cts,
            IReadOnlyDictionary<Guid, IEnumerable<string>> usages)
        {
            return cts.Select(x => new DataTypeReferences.ContentTypeReferences
            {
                Id = x.Id,
                Key = x.Key,
                Alias = x.Alias,
                Icon = x.Icon,
                Name = x.Name,
                Udi = new GuidUdi(ObjectTypes.GetUdiType(UmbracoObjectTypes.DocumentType), x.Key),
                //only select matching properties
                Properties = x.PropertyTypes.Where(p => usages[x.Key].InvariantContains(p.Alias))
                    .Select(p => new DataTypeReferences.ContentTypeReferences.PropertyTypeReferences
                    {
                        Alias = p.Alias,
                        Name = p.Name
                    })
            });
        }

        #region ReadOnly actions to return basic data - allow access for: content ,media, members, settings, developer
        /// <summary>
        /// Gets the content json for all data types
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Permission is granted to this method if the user has access to any of these sections: Content, media, settings, developer, members
        /// </remarks>
        [UmbracoApplicationAuthorizeAttribute(Constants.Applications.Content, Constants.Applications.Media, Constants.Applications.Members,
            Constants.Applications.Settings, Constants.Applications.Packages)]
        public IEnumerable<DataTypeBasic> GetAll()
        {
            return _dataTypeService
                     .GetAll()
                     .Select(_umbracoMapper.Map<IDataType, DataTypeBasic>).Where(x => x.IsSystemDataType == false);
        }

        /// <summary>
        /// Returns all data types grouped by their property editor group
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Permission is granted to this method if the user has access to any of these sections: Content, media, settings, developer, members
        /// </remarks>
        [UmbracoTreeAuthorizeAttribute(Constants.Applications.Content, Constants.Applications.Media, Constants.Applications.Members,
            Constants.Applications.Settings, Constants.Applications.Packages)]
        public IDictionary<string, IEnumerable<DataTypeBasic>> GetGroupedDataTypes()
        {
            var dataTypes = _dataTypeService
                     .GetAll()
                     .Select(_umbracoMapper.Map<IDataType, DataTypeBasic>)
                     .ToArray();

            var propertyEditors =_propertyEditorCollection.ToArray();

            foreach (var dataType in dataTypes)
            {
                var propertyEditor = propertyEditors.SingleOrDefault(x => x.Alias == dataType.Alias);
                if (propertyEditor != null)
                    dataType.HasPrevalues = propertyEditor.GetConfigurationEditor().Fields.Any();
            }

            var grouped = dataTypes
                .GroupBy(x => x.Group.IsNullOrWhiteSpace() ? "" : x.Group.ToLower())
                .ToDictionary(group => group.Key, group => group.OrderBy(d => d.Name).AsEnumerable());

            return grouped;
        }

        /// <summary>
        /// Returns all property editors grouped
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Permission is granted to this method if the user has access to any of these sections: Content, media, settings, developer, members
        /// </remarks>
        [UmbracoTreeAuthorizeAttribute(Constants.Applications.Content, Constants.Applications.Media, Constants.Applications.Members,
            Constants.Applications.Settings, Constants.Applications.Packages)]

        public IDictionary<string, IEnumerable<DataTypeBasic>> GetGroupedPropertyEditors()
        {
            var datatypes = new List<DataTypeBasic>();
            var showDeprecatedPropertyEditors = _contentSettings.ShowDeprecatedPropertyEditors;

            var propertyEditors =_propertyEditorCollection
                .Where(x=>x.IsDeprecated == false || showDeprecatedPropertyEditors);
            foreach (var propertyEditor in propertyEditors)
            {
                var hasPrevalues = propertyEditor.GetConfigurationEditor().Fields.Any();
                var basic = _umbracoMapper.Map<DataTypeBasic>(propertyEditor);
                basic.HasPrevalues = hasPrevalues;
                datatypes.Add(basic);
            }

            var grouped = datatypes
                .GroupBy(x => x.Group.IsNullOrWhiteSpace() ? "" : x.Group.ToLower())
                .ToDictionary(group => group.Key, group => group.OrderBy(d => d.Name).AsEnumerable());

            return grouped;
        }


        /// <summary>
        /// Gets all property editors defined
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// Permission is granted to this method if the user has access to any of these sections: Content, media, settings, developer, members
        /// </remarks>
        [UmbracoTreeAuthorizeAttribute(Constants.Applications.Content, Constants.Applications.Media, Constants.Applications.Members,
            Constants.Applications.Settings, Constants.Applications.Packages)]

        public IEnumerable<PropertyEditorBasic> GetAllPropertyEditors()
        {
            return _propertyEditorCollection
                .OrderBy(x => x.Name)
                .Select(_umbracoMapper.Map<PropertyEditorBasic>);
        }
        #endregion
    }
}