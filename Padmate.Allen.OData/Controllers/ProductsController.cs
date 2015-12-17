using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Routing;
using Padmate.Allen.OData.Models;
using System.Web.Http.Routing;
using Microsoft.Data.OData.Query;
using Microsoft.Data.OData;

namespace Padmate.Allen.OData.Controllers
{
    /*
    在为此控制器添加路由之前，WebApiConfig 类可能要求你做出其他更改。请适当地将这些语句合并到 WebApiConfig 类的 Register 方法中。请注意 OData URL 区分大小写。

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using Padmate.Allen.OData.Models;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Product>("Products");
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
    public class ProductsController : ODataController
    {
        private ProductServiceContext db = new ProductServiceContext();

        [HttpPost]
        public Product CheckOut([FromODataUri] int key)
        {
            var movie = db.Products.Find(key);
            if (movie == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

           
            return movie;
        }


        [HttpPost]
        public async Task<IHttpActionResult> RateProduct([FromODataUri] int key, ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            int rating = (int)parameters["Rating"];

            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            product.Ratings.Add(new ProductRating() { Rating = rating });
            db.SaveChanges();

            double average = product.Ratings.Average(x => x.Rating);

            return Ok(average);
        }

        [HttpPost]
        public void RateAllProducts(ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var ratings = parameters["Ratings"] as ICollection<int>;

            // ...
        }

        // GET /Products(1)/Supplier
        public Supplier GetSupplier([FromODataUri] int key)
        {
            Product product = db.Products.FirstOrDefault(p => p.ID == key);
            if (product == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            return product.Supplier;
        }

        [AcceptVerbs("POST", "PUT")]
        public async Task<IHttpActionResult> CreateLink([FromODataUri] int key, string navigationProperty, [FromBody] Uri link)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            switch (navigationProperty)
            {
                case "Supplier":
                    string supplierKey = GetKeyFromLinkUri<string>(link);
                    Supplier supplier = await db.Suppliers.FindAsync(supplierKey);
                    if (supplier == null)
                    {
                        return NotFound();
                    }
                    product.Supplier = supplier;
                    await db.SaveChangesAsync();
                    return StatusCode(HttpStatusCode.NoContent);

                default:
                    return NotFound();
            }
        }

        // Helper method to extract the key from an OData link URI.
        private TKey GetKeyFromLinkUri<TKey>(Uri link)
        {
            TKey key = default(TKey);

            // Get the route that was used for this request.
            IHttpRoute route = Request.GetRouteData().Route;

            // Create an equivalent self-hosted route. 
            IHttpRoute newRoute = new HttpRoute(route.RouteTemplate,
                new HttpRouteValueDictionary(route.Defaults),
                new HttpRouteValueDictionary(route.Constraints),
                new HttpRouteValueDictionary(route.DataTokens), route.Handler);

            // Create a fake GET request for the link URI.
            var tmpRequest = new HttpRequestMessage(HttpMethod.Get, link);

            // Send this request through the routing process.
            var routeData = newRoute.GetRouteData(
                Request.GetConfiguration().VirtualPathRoot, tmpRequest);

            // If the GET request matches the route, use the path segments to find the key.
            if (routeData != null)
            {
                ODataPath path = tmpRequest.GetODataPath();
                var segment = path.Segments.OfType<KeyValuePathSegment>().FirstOrDefault();
                if (segment != null)
                {
                    // Convert the segment into the key type.
                    key = (TKey)ODataUriUtils.ConvertFromUriLiteral(
                        segment.Value, ODataVersion.V3);
                }
            }
            return key;
        }

        // GET: odata/Products
        [EnableQuery]
        public IQueryable<Product> GetProducts()
        {
            return db.Products;
        }

        // GET: odata/Products(5)
        [EnableQuery]
        public SingleResult<Product> GetProduct([FromODataUri] int key)
        {
            return SingleResult.Create(db.Products.Where(product => product.ID == key));
        }

        // PUT: odata/Products(5)
        public async Task<IHttpActionResult> Put([FromODataUri] int key, Delta<Product> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            patch.Put(product);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(product);
        }

        // POST: odata/Products
        public async Task<IHttpActionResult> Post(Product product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Products.Add(product);
            await db.SaveChangesAsync();

            return Created(product);
        }

        // PATCH: odata/Products(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public async Task<IHttpActionResult> Patch([FromODataUri] int key, Delta<Product> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            patch.Patch(product);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(product);
        }

        // DELETE: odata/Products(5)
        public async Task<IHttpActionResult> Delete([FromODataUri] int key)
        {
            Product product = await db.Products.FindAsync(key);
            if (product == null)
            {
                return NotFound();
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool ProductExists(int key)
        {
            return db.Products.Count(e => e.ID == key) > 0;
        }
    }
}
