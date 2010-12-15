<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="indexTitle" ContentPlaceHolderID="TitleContent" runat="server">
    Home Page
</asp:Content>

<asp:Content ID="indexContent" ContentPlaceHolderID="MainContent" runat="server">

    <h2>Welcome to the Xero API Sample Web Application!</h2>
    
    <%if(Session["oauth_access_token"] == null) {%>
    <p>
        You are not currently connected to the Xero API. To connect to the API, click <%=Html.ActionLink("here", "Index", "Connect") %>
    </p> 
        
    <% } else { %>   
    <p>
        You are currently connected to the Xero API! 
        Use the menu buttons at the top of this page to read/write data to the authorised organisation.
    </p>
    
    <div>
        <div>Consumer Key: <%=ConfigurationManager.AppSettings["XeroApiConsumerKey"]%></div>
        <div>Consumer Secret: <%=ConfigurationManager.AppSettings["XeroApiConsumerSecret"]%></div>
        
        <div>Access Key: <%=Session["oauth_access_token"]%></div>
        <div>Access Secret: <%=Session["oauth_access_secret"]%></div>
        
        <div>Authorised Organisation: <%=Session["xero_organisation_name"]%></div>
        <div>The OAuth connection was made on <%=Session["xero_connection_time"]%></div>
    </div>
    <% } %>
    
    <p>
        Note, This website is currently running under the user <strong><%=Environment.UserDomainName %>\<%=Environment.UserName %></strong>. 
        If this website needs to access certificates in the local machine store, this user (or a group containing this user) must have access 
        to any certificates that this website uses.
    </p>
    
</asp:Content>
