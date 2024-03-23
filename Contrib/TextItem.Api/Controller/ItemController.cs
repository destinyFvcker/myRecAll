﻿using Microsoft.AspNetCore.Mvc;
using RecAll.Contrib.TextItem.Api.Data;
using RecAll.Contrib.TextItem.Api.Service;
using RecAll.Contrib.TextItem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace RecAll.Contrib.TextItem.Api.Controller;

[ApiController]
[Route("[controller]")]
public class ItemController
{
    private readonly IIdentityService _identityService;
    private readonly TextItemContext _textItemContext;

    public ItemController(IIdentityService identityService,
                            TextItemContext textItemContext)
    {
        _identityService = identityService;
        _textItemContext = textItemContext;
    }

    [Route("create")]
    [HttpPost]
    public async Task<ActionResult<string>> CreateAsync(
           [FromBody] CreateTextItemCommand command)
    {
        var textItem = new Models.TextItem
        {
            Content = command.Content,
            UserIdentityGuid = _identityService.GetUserIdentityGuid(),
            IsDeleted = false,
        };

        var textItemEntity = _textItemContext.Add(textItem);
        await _textItemContext.SaveChangesAsync();
        return textItemEntity.Entity.Id.ToString();
    }

    [Route("update")]
    [HttpPost]
    public async Task<ActionResult> UpdateAsync(
        [FromBody] UpdateTextItemCommand command
    )
    {
        var userIdentityGuid = _identityService.GetUserIdentityGuid();

        var textItem = await _textItemContext.TextItems.FirstOrDefaultAsync(p =>
            p.Id == command.Id &&
            p.UserIdentityGuid == userIdentityGuid &&
            !p.IsDeleted);

        if (textItem is null)
        {
            return new BadRequestResult();
        }

        textItem.Content = command.Content;
        await _textItemContext.SaveChangesAsync();

        return new OkResult();
    }

    [Route("get/{id}")]
    [HttpGet]
    public async Task<ActionResult<Models.TextItem>> GetAsync(int id)
    {
        var userIdentityGuid = _identityService.GetUserIdentityGuid();

        var textItem = await _textItemContext.TextItems.FirstOrDefaultAsync(p =>
            p.Id == id &&
            p.UserIdentityGuid == userIdentityGuid &&
            !p.IsDeleted
        );

        return textItem is null ? new BadRequestResult() : textItem;
    }

    [Route("getByItemId/{itemId}")]
    [HttpGet]
    public async Task<ActionResult<Models.TextItem>> GetByItemId(int itemId)
    {
        var userIdentityGuid = _identityService.GetUserIdentityGuid();

        var textItem = await _textItemContext.TextItems.FirstOrDefaultAsync(p =>
            p.ItemId == itemId &&
            p.UserIdentityGuid == userIdentityGuid &&
            !p.IsDeleted
        );

        return textItem is null ? new BadRequestResult() : textItem;
    }

    [Route("getItems")]
    [HttpPost]
    public async Task<ActionResult<IEnumerable<Models.TextItem>>> GetItemsAsync(
        GetItemsCommand command
    )
    {
        var itemIds = command.Ids.ToList();
        var userIdentityGuid = _identityService.GetUserIdentityGuid();

        var textItems = await _textItemContext.TextItems.Where(p =>
            p.ItemId.HasValue &&
            itemIds.Contains(p.ItemId.Value) &&
            p.UserIdentityGuid == userIdentityGuid &&
            !p.IsDeleted
        ).ToListAsync();

        if (textItems.Count != itemIds.Count)
        {
            var missingIds = string.Join(",",
                itemIds.Except(textItems.Select(p => p.ItemId.Value))
                    .Select(p => p.ToString()));

            return new BadRequestResult();
        }

        textItems.Sort((x, y) =>
            itemIds.IndexOf(x.ItemId.Value) - itemIds.IndexOf(y.ItemId.Value));

        return textItems;
    }
}
