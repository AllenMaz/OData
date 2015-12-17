using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Extensions;
using Padmate.Allen.OData.Models;
using System.Web.Http.OData.Routing;

namespace Padmate.Allen.OData
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API 配置和服务

            // Web API 路由
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
            builder.EntitySet<Product>("Products");
            builder.EntitySet<Supplier>("Suppliers");
            builder.EntitySet<ProductRating> ("Ratings");

            // New code: Add an action to the EDM, and define the parameter and return type.
            ActionConfiguration rateProduct = builder.Entity<Product>().Action("RateProduct");
            rateProduct.Parameter<int>("Rating");
            rateProduct.Returns<double>();

            ActionConfiguration rateAllProducts = builder.Entity<Product>().Collection.Action("RateAllProducts");
            rateAllProducts.CollectionParameter<int>("Ratings");
            rateAllProducts.CollectionParameter<string>("from");
            rateAllProducts.Returns<double>();

            //瞬态Action
            var checkoutAction = builder.Entity<Product>().TransientAction("CheckOut");
            checkoutAction.HasActionLink(ctx =>
            {
                var product = ctx.EntityInstance as Product;
                if (true)
                {
                    var returnuri= new Uri(ctx.Url.ODataLink(
                        new EntitySetPathSegment(ctx.EntitySet),
                        new KeyValuePathSegment(product.ID.ToString()),
                        new ActionPathSegment(checkoutAction.Name)));
                    return returnuri;
                }
                else
                {
                    return null;
                }
            }, followsConventions: true);
            checkoutAction.ReturnsFromEntitySet<Product>("Products");

            config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
        }
    }
}
