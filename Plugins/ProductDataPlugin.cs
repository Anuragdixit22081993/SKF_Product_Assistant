using System.ComponentModel;
using Microsoft.SemanticKernel;
using SKF_Product_Assistant.Services;

namespace SKF_Product_Assistant.Plugins;

public sealed class ProductDataPlugin
{
    private readonly ProductDataService _productDataService;

    public ProductDataPlugin(ProductDataService productDataService)
    {
        _productDataService = productDataService;
    }

    [KernelFunction("get_product_attribute")]
    [Description("Gets a product attribute value from the official SKF product datasheet.")]
    public async Task<string> GetProductAttributeAsync(
        [Description("The exact product designation, for example 6205 or 6205 N.")]
        string designation,
        [Description("The requested attribute, for example Width, Bore diameter, or Outside diameter.")]
        string attribute)
    {
        var value = await _productDataService.GetProductAttributeAsync(
            designation,
            attribute);

        if (value is null)
        {
            return $"No value was found for attribute '{attribute}' for product '{designation}'.";
        }

        return value;
    }
}