<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
	Done
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

    <h2>Authorization Complete!</h2>

    <p>
        This application is now connected to the Xero API.
    </p>

    <p>
        <span>The Access Token for this session is: <%=Session["oauth_session_token"]%></span>
        <br />
        <span>The corresponding Secret for this session is: <%=Session["oauth_session_secret"]%></span>
    </p>

    <p>
        Use the menu buttons at the top of this page to read/write data to the current authorised organisation.
    </p>

</asp:Content>
