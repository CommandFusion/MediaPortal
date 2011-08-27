Imports MediaPortal.Plugins.MovingPictures.Database
Imports MediaPortal.Player
Imports MediaPortal.Plugins.MovingPictures
Imports MediaPortal.GUI.Library
Imports System.Threading

Public NotInheritable Class Movies

    ' Gets a list of videos from the Moving Pictures database.
    Public Shared Function GetAllMovies(Optional ByVal PerRow As Integer = 1) As String

        Dim allMovies As New List(Of DBMovieInfo)
        allMovies = DBMovieInfo.GetAll
        allMovies.Sort()

        Dim response As String = "\xF3RLISTMOVIES\xF4start|" & allMovies.Count & "|" & PerRow & "\xF5\xF5"

        For i As Integer = 0 To allMovies.Count - 1 Step PerRow

            response &= "\xF3RLISTMOVIES\xF4"

            For j As Integer = 0 To PerRow - 1

                If allMovies.Count > (i + j) Then
                    Dim movieInfo As DBMovieInfo = allMovies(i + j)

                    Dim file As String = ""
                    For Each mediaFile As DBLocalMedia In movieInfo.LocalMedia
                        file = file & mediaFile.FullPath & ";"
                    Next

                    ' item|<index>|<ID>|<title>|<year>
                    response &= "item|" & i + j & "|" & movieInfo.ID & "|" & movieInfo.Title.Replace("|", ":") & "|" & movieInfo.Year & "|"
                End If
            Next

            response = response.Substring(0, response.Length - 1)
            response &= "\xF5\xF5"
        Next

        response &= "\xF3RLISTMOVIES\xF4end\xF5\xF5"
        Return response


    End Function

    Public Shared Function Search(ByVal q As String, Optional ByVal PerRow As Integer = 1) As String
        If q.Trim = "" Then
            Return GetAllMovies()
        Else
            Dim allMovies As New List(Of DBMovieInfo)
            Dim filteredMovies As New List(Of DBMovieInfo)
            allMovies = DBMovieInfo.GetAll

            For Each aMovie As DBMovieInfo In allMovies
                If aMovie.Title.Contains(q) Then
                    filteredMovies.Add(aMovie)
                End If
            Next

            ' Check if any movies were found
            If filteredMovies.Count < 1 Then
                ' No movies found, so now search other aspects of the movie
                For Each aMovie As DBMovieInfo In allMovies
                    If aMovie.Summary.Contains(q) Or aMovie.Tagline.Contains(q) Or aMovie.Year = Integer.Parse(q) Then
                        filteredMovies.Add(aMovie)
                    End If
                Next
            End If

            If filteredMovies.Count > 0 Then
                filteredMovies.Sort()
                Dim response As String = "\xF3RLISTMOVIES\xF4start|" & filteredMovies.Count & "|" & PerRow & "\xF5\xF5"

                For i As Integer = 0 To filteredMovies.Count - 1 Step PerRow

                    response &= "\xF3RLISTMOVIES\xF4"

                    For j As Integer = 0 To PerRow - 1

                        If filteredMovies.Count > (i + j) Then
                            Dim movieInfo As DBMovieInfo = filteredMovies(i + j)

                            Dim file As String = ""
                            For Each mediaFile As DBLocalMedia In movieInfo.LocalMedia
                                file = file & mediaFile.FullPath & ";"
                            Next

                            ' item|<index>|<ID>|<title>|<year>
                            response &= "item|" & i + j & "|" & movieInfo.ID & "|" & movieInfo.Title.Replace("|", ":") & "|" & movieInfo.Year & "|"
                        End If
                    Next

                    response = response.Substring(0, response.Length - 1)
                    response &= "\xF5\xF5"
                Next

                response &= "\xF3RLISTMOVIES\xF4end\xF5\xF5"
                Return response
            End If

            Return "\xF3RSEARCHERR\xF4No movies found matching search criteria\xF5\xF5"

        End If
    End Function

    ' Gets video information from the native MediaPortal MovingPictures database
    Public Shared Function GetVideoInfo(ByVal ID As Integer) As String

        Dim movieInfo As New DBMovieInfo
        movieInfo = DBMovieInfo.Get(ID)

        ' Format:
        ' <ID>|<title>|<year>|<tagline>|<plot>|<runtime>|<rating>
        Return "\xF3RMOVIEINFO\xF4" & movieInfo.ID & "|" & movieInfo.Title.Replace("|", ":") & "|" & movieInfo.Year & "|" & movieInfo.Tagline.Replace("|", ":") & "|" & movieInfo.Summary.Replace("|", ":") & "|" & movieInfo.Runtime & "|" & movieInfo.Score & "\xF5\xF5"

    End Function

    Public Shared Sub PlayMovie(ByVal ID As Integer)

        Dim movieInfo As New DBMovieInfo
        movieInfo = DBMovieInfo.Get(ID)
        Dim movie As New Movie
        movie.movieInfo = movieInfo
        movie.PlayVideo()

    End Sub

    Public Shared Function GetThumb(ByVal ID As Integer) As String
        Dim movie As New DBMovieInfo
        movie = DBMovieInfo.Get(ID)

        If movie IsNot Nothing Then
            Return movie.CoverThumbFullPath
        Else
            Return Nothing
        End If
    End Function

    Public Shared Function GetFanart(ByVal ID As Integer) As String
        Dim movie As New DBMovieInfo
        movie = DBMovieInfo.Get(ID)

        Return movie.BackdropFullPath

    End Function

End Class
