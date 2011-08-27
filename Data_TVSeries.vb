Imports WindowPlugins.GUITVSeries
Imports MediaPortal.Player
Imports System.Threading

Public NotInheritable Class TVSeries
    Public Shared Function GetAllSeries(Optional ByVal listStart As Integer = 0, Optional ByVal listCount As Integer = 0) As String

        Dim sqlCondition As New SQLCondition
        sqlCondition.Add(New DBOnlineSeries(), DBOnlineSeries.cViewTags, "", SQLConditionType.Like)
        sqlCondition.AddOrderItem(DBOnlineSeries.Q(DBOnlineSeries.cPrettyName), sqlCondition.orderType.Ascending)
        If listCount > 0 Then
            sqlCondition.SetLimit(listCount)
        End If

        Dim seriesList As List(Of DBSeries) = DBSeries.Get(sqlCondition)

        Dim response As String = "\xF3RGETALLSERIES\xF4start|" & seriesList.Count & "\xF5\xF5"

        Dim i As Integer = 0
        For Each series As DBSeries In seriesList
            response &= "\xF3RGETALLSERIES\xF4item|" & i & "|" & series.Item("ID").ToString & "|" & series.Item("Pretty_Name").ToString & "\xF5\xF5"
            i += 1
        Next
        response &= "\xF3RGETALLSERIES\xF4end\xF5\xF5"
        Return response
    End Function

    Public Shared Function GetSeriesArtworkPath(ByVal seriesID As Integer) As String
        Dim series As DBSeries = DBSeries.Get(CInt(seriesID))
        'Dim artwork As String = Fanart.getFanart(series.Item("ID")).FanartFilename
        Dim artwork As String = series.Banner
        If IO.File.Exists(artwork) Then
            Return artwork
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetSeriesFanArtworkPath(ByVal seriesID As Integer) As String
        Dim series As DBSeries = DBSeries.Get(CInt(seriesID))
        Dim artwork As String = Fanart.getFanart(series.Item("ID")).FanartFilename
        If IO.File.Exists(artwork) Then
            Return artwork
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetAllSeasons(ByVal seriesID As Integer) As String

        Dim series As DBSeries = DBSeries.Get(CInt(seriesID))
        Dim seasonList As List(Of DBSeason) = DBSeason.Get(CInt(seriesID))

        Dim response As String = "\xF3RGETALLSEASONS\xF4start|" & seasonList.Count & "|" & series.Item(DBSeries.cID).ToString & "|" & series.Item("Pretty_Name").ToString & "\xF5\xF5"

        Dim i As Integer = 0
        For Each season As DBSeason In seasonList
            response &= "\xF3RGETALLSEASONS\xF4item|" & i & "|" & season.Item(DBSeason.cID).ToString & "|" & season.Item(DBSeason.cIndex).ToString & "|" & season.Item(DBSeason.cEpisodeCount).ToString & "\xF5\xF5"
            i += 1
        Next
        response &= "\xF3RGETALLSEASONS\xF4end\xF5\xF5"
        Return response
    End Function

    Public Shared Function GetAllEpisodes(ByVal seriesID As Integer, ByVal seasonIndex As Integer) As String

        Dim epList As List(Of DBEpisode) = DBEpisode.Get(CInt(seriesID), CInt(seasonIndex))
        Dim series As DBSeries = DBSeries.Get(CInt(seriesID))

        Dim response As String = "\xF3RGETALLEPISODES\xF4start|" & epList.Count & "|" & series.Item(DBSeries.cID).ToString & "|" & series.Item("Pretty_Name").ToString & "|" & seasonIndex & "\xF5\xF5"

        Dim i As Integer = 0
        For Each episode As DBEpisode In epList
            response &= "\xF3RGETALLEPISODES\xF4item|" & i & "|" & episode.Item(DBEpisode.cEpisodeIndex).ToString & "|" & episode.Item(DBEpisode.cEpisodeName).ToString & "|" & episode.Item(DBEpisode.cPrettyPlaytime).ToString & "\xF5\xF5"
            i += 1
        Next
        response &= "\xF3RGETALLEPISODES\xF4end\xF5\xF5"
        Return response
    End Function

    Public Shared Function GetEpisode(ByVal seriesID As Integer, ByVal seasonIndex As Integer, ByVal epIndex As Integer) As DBEpisode
        Dim episodeList As List(Of DBEpisode) = DBEpisode.Get(seriesID, seasonIndex)

        Dim episode As DBEpisode = Nothing
        For Each episode In episodeList
            If CInt(episode.Item(DBEpisode.cEpisodeIndex)) = epIndex Then
                Exit For
            End If
        Next

        Return episode

    End Function

    Public Shared Function GetEpisodeArtworkPath(ByVal theEp As DBEpisode) As String
        Dim artwork As String = theEp.Image
        If IO.File.Exists(artwork) Then
            Return artwork
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetPlayingEpisodeArtworkPath() As String
        Dim sqlCondition As New SQLCondition
        sqlCondition.Add(New DBEpisode(), DBEpisode.cFilename, g_Player.Player.CurrentFile.ToString, SQLConditionType.Equal)
        Dim episodeList As List(Of DBEpisode) = DBEpisode.Get(sqlCondition)
        Return GetEpisodeArtworkPath(episodeList(0))
    End Function

    Public Shared Function GetPlayingFanArtworkPath() As String
        Dim sqlCondition As New SQLCondition
        sqlCondition.Add(New DBEpisode(), DBEpisode.cFilename, g_Player.Player.CurrentFile.ToString, SQLConditionType.Equal)
        Dim episodeList As List(Of DBEpisode) = DBEpisode.Get(sqlCondition)
        Return GetSeriesFanArtworkPath(episodeList(0).Item(DBEpisode.cSeriesID))
    End Function
End Class
