<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Xero.ScreencastWeb.Models"%>
<%@ Import Namespace="Xero.ScreencastWeb.Services"%>
<div>
    <%
    ApiRepository apiRepository = new ApiRepository();
    Response response = apiRepository.GetItemByIdOrCode<Organisation>(Session, "");
   
    if (response.Status == "OK") {
    %>
        <span>Connected to: <%=response.Organisations[0].Name %></span>
    <% } else { %>
        <span>Not Connected</span>
    <% } %>
</div>