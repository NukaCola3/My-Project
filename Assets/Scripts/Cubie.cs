using System;
using System.Collections.Generic;
using UnityEngine;


// ==================================================
// CUBIE-TYP
// ==================================================

public enum CubieType
{
    Corner,
    Edge,
    Center
}


// ==================================================
// FLÄCHENRICHTUNG
// ==================================================

public enum FaceDirection
{
    PositiveX,
    NegativeX,

    PositiveY,
    NegativeY,

    PositiveZ,
    NegativeZ
}


// ==================================================
// STICKER
// ==================================================

[Serializable]
public class CubieSticker
{
    public FaceDirection originalDirection;
    public FaceDirection currentDirection;

    public CubieSticker(FaceDirection direction)
    {
        originalDirection = direction;
        currentDirection = direction;
    }
}


// ==================================================
// CUBIE
// ==================================================

public class Cubie : MonoBehaviour
{
    // ==================================================
    // LOGISCHE DATEN
    // ==================================================

    [Header("Logical Position")]
    public Vector3Int logicalPosition;

    [HideInInspector]
    public Vector3Int originalPosition;


    [Header("Logical Rotation")]
    public Quaternion logicalRotation =
        Quaternion.identity;


    // ==================================================
    // IDENTITÄT
    // ==================================================

    [Header("Piece ID")]
    public string pieceID;


    // ==================================================
    // ORIENTIERUNG
    // ==================================================

    [Header("Orientation")]
    [Range(0, 2)]
    public int orientation = 0;


    // ==================================================
    // STICKER
    // ==================================================

    [Header("Stickers")]
    public List<CubieSticker> stickers =
        new List<CubieSticker>();


    // ==================================================
    // CUBIE-TYP
    // ==================================================

    public CubieType Type
    {
        get
        {
            int coordinateSum =
                Mathf.Abs(logicalPosition.x) +
                Mathf.Abs(logicalPosition.y) +
                Mathf.Abs(logicalPosition.z);

            if (coordinateSum == 3)
            {
                return CubieType.Corner;
            }

            if (coordinateSum == 2)
            {
                return CubieType.Edge;
            }

            return CubieType.Center;
        }
    }


    // ==================================================
    // AWAKE
    // ==================================================

    private void Awake()
    {
        Initialize();
    }


    // ==================================================
    // INITIALISIERUNG
    // ==================================================

    private void Initialize()
    {
        // ------------------------------------------
        // LOGISCHE POSITION
        // ------------------------------------------

        logicalPosition =
            new Vector3Int(
                Mathf.RoundToInt(transform.localPosition.x),
                Mathf.RoundToInt(transform.localPosition.y),
                Mathf.RoundToInt(transform.localPosition.z)
            );

        originalPosition =
            logicalPosition;


        // ------------------------------------------
        // LOGISCHE ROTATION
        // ------------------------------------------

        logicalRotation =
            transform.localRotation;


        // ------------------------------------------
        // STICKER ERZEUGEN
        // ------------------------------------------

        InitializeStickers();


        // ------------------------------------------
        // PIECE-ID ERZEUGEN
        // ------------------------------------------

        GeneratePieceID();


        // ------------------------------------------
        // ORIENTIERUNG
        // ------------------------------------------

        orientation = 0;
    }


    // ==================================================
    // STICKER INITIALISIEREN
    // ==================================================

    private void InitializeStickers()
    {
        stickers.Clear();


        // ------------------------------------------
        // X-Achse
        // ------------------------------------------

        if (logicalPosition.x > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveX
                )
            );
        }

        if (logicalPosition.x < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeX
                )
            );
        }


        // ------------------------------------------
        // Y-Achse
        // ------------------------------------------

        if (logicalPosition.y > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveY
                )
            );
        }

        if (logicalPosition.y < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeY
                )
            );
        }


        // ------------------------------------------
        // Z-Achse
        // ------------------------------------------

        if (logicalPosition.z > 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.PositiveZ
                )
            );
        }

        if (logicalPosition.z < 0)
        {
            stickers.Add(
                new CubieSticker(
                    FaceDirection.NegativeZ
                )
            );
        }
    }


    // ==================================================
    // PIECE-ID
    // ==================================================

    private void GeneratePieceID()
    {
        string id = "";


        // ------------------------------------------
        // Y
        // ------------------------------------------

        if (logicalPosition.y > 0)
        {
            id += "U";
        }
        else if (logicalPosition.y < 0)
        {
            id += "D";
        }


        // ------------------------------------------
        // X
        // ------------------------------------------

        if (logicalPosition.x > 0)
        {
            id += "R";
        }
        else if (logicalPosition.x < 0)
        {
            id += "L";
        }


        // ------------------------------------------
        // Z
        // ------------------------------------------

        if (logicalPosition.z > 0)
        {
            id += "F";
        }
        else if (logicalPosition.z < 0)
        {
            id += "B";
        }


        pieceID = id;
    }


    // ==================================================
    // LOGISCHE ROTATION AKTUALISIEREN
    // ==================================================
    //
    // Wird von RubiksCube.cs aufgerufen:
    //
    // UpdateLogicalRotation(
    //     rotationAxis,
    //     direction
    // );
    //
    // rotationAxis ist dort ein Vector3.
    // ==================================================

    public void UpdateLogicalRotation(
        Vector3 rotationAxis,
        int direction)
    {
        Quaternion rotation =
            Quaternion.AngleAxis(
                90f * direction,
                rotationAxis
            );

        logicalRotation =
            rotation * logicalRotation;

        logicalRotation =
            Quaternion.Normalize(
                logicalRotation
            );
    }


    // ==================================================
    // ALTERNATIVE ROTATIONS-METHODE
    // ==================================================

    public void UpdateLogicalRotation(
        Quaternion rotation)
    {
        logicalRotation =
            rotation * logicalRotation;

        logicalRotation =
            Quaternion.Normalize(
                logicalRotation
            );
    }


    // ==================================================
    // STICKER ROTIEREN
    // ==================================================

    public void RotateStickers(
        RotationAxis axis,
        int direction)
    {
        foreach (CubieSticker sticker in stickers)
        {
            sticker.currentDirection =
                RotateFaceDirection(
                    sticker.currentDirection,
                    axis,
                    direction
                );
        }
    }


    // ==================================================
    // FACE-RICHTUNG ROTIEREN
    // ==================================================

    private FaceDirection RotateFaceDirection(
        FaceDirection face,
        RotationAxis axis,
        int direction)
    {
        // ------------------------------------------
        // X-ACHSE
        // ------------------------------------------

        if (axis == RotationAxis.X)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveY:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.PositiveY;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveY:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.PositiveY;
                }
            }
        }


        // ------------------------------------------
        // Y-ACHSE
        // ------------------------------------------

        if (axis == RotationAxis.Y)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.PositiveX;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.NegativeZ;

                    case FaceDirection.NegativeZ:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.PositiveZ;

                    case FaceDirection.PositiveZ:
                        return FaceDirection.PositiveX;
                }
            }
        }


        // ------------------------------------------
        // Z-ACHSE
        // ------------------------------------------

        if (axis == RotationAxis.Z)
        {
            if (direction == 1)
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.PositiveY;

                    case FaceDirection.PositiveY:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.PositiveX;
                }
            }
            else
            {
                switch (face)
                {
                    case FaceDirection.PositiveX:
                        return FaceDirection.NegativeY;

                    case FaceDirection.NegativeY:
                        return FaceDirection.NegativeX;

                    case FaceDirection.NegativeX:
                        return FaceDirection.PositiveY;

                    case FaceDirection.PositiveY:
                        return FaceDirection.PositiveX;
                }
            }
        }


        return face;
    }


    // ==================================================
    // ORIENTIERUNG AKTUALISIEREN
    // ==================================================

    public void UpdateOrientation()
    {
        switch (Type)
        {
            case CubieType.Corner:
                UpdateCornerOrientation();
                break;

            case CubieType.Edge:
                UpdateEdgeOrientation();
                break;

            case CubieType.Center:
                orientation = 0;
                break;
        }
    }


    // ==================================================
    // CORNER ORIENTIERUNG
    // ==================================================
    //
    // WICHTIG:
    //
    // Die Orientierung einer Ecke wird anhand des
    // ursprünglichen U/D-Stickers bestimmt.
    //
    // Dabei reicht es NICHT aus, nur die aktuelle Achse
    // des Stickers anzusehen.
    //
    // Wir müssen zusätzlich berücksichtigen, ob sich
    // die Ecke aktuell in der U- oder D-Schicht befindet.
    //
    // Definition:
    //
    // U/D-Sticker auf U/D:
    //     Orientierung = 0
    //
    // U/D-Sticker auf X-Achse:
    //     U-Schicht -> 1
    //     D-Schicht -> 2
    //
    // U/D-Sticker auf Z-Achse:
    //     U-Schicht -> 2
    //     D-Schicht -> 1
    //
    // Dadurch erhält jede gültige Würfelstellung eine
    // Corner-Orientierungssumme, die durch 3 teilbar ist.
    // ==================================================

    private void UpdateCornerOrientation()
    {
        orientation = 0;


        // ------------------------------------------
        // U/D-STICKER SUCHEN
        // ------------------------------------------

        CubieSticker udSticker = null;

        foreach (CubieSticker sticker in stickers)
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveY ||
                sticker.originalDirection ==
                    FaceDirection.NegativeY
            )
            {
                udSticker = sticker;
                break;
            }
        }


        // Keine U/D-Sticker gefunden:
        // Das wäre für einen Corner ungültig.
        if (udSticker == null)
        {
            orientation = 0;
            return;
        }


        FaceDirection current =
            udSticker.currentDirection;


        // ------------------------------------------
        // FALL 1:
        // U/D-STICKER IST NOCH AUF U ODER D
        // ------------------------------------------

        if (
            current ==
                FaceDirection.PositiveY ||
            current ==
                FaceDirection.NegativeY
        )
        {
            orientation = 0;
            return;
        }


        // ------------------------------------------
        // DIE AKTUELLE SCHICHT BESTIMMEN
        // ------------------------------------------
        //
        // Ein Corner befindet sich immer entweder
        // in der U-Schicht (y > 0) oder D-Schicht (y < 0).
        //
        // Das ist entscheidend für die Unterscheidung
        // zwischen Orientierung 1 und 2.
        // ------------------------------------------

        bool isUpperLayer =
            logicalPosition.y > 0;


        // ------------------------------------------
        // FALL 2:
        // U/D-STICKER IST AUF DER X-ACHSE
        // ------------------------------------------

        if (
            current ==
                FaceDirection.PositiveX ||
            current ==
                FaceDirection.NegativeX
        )
        {
            if (isUpperLayer)
            {
                orientation = 1;
            }
            else
            {
                orientation = 2;
            }

            return;
        }


        // ------------------------------------------
        // FALL 3:
        // U/D-STICKER IST AUF DER Z-ACHSE
        // ------------------------------------------

        if (
            current ==
                FaceDirection.PositiveZ ||
            current ==
                FaceDirection.NegativeZ
        )
        {
            if (isUpperLayer)
            {
                orientation = 2;
            }
            else
            {
                orientation = 1;
            }

            return;
        }


        // ------------------------------------------
        // FALLBACK
        // ------------------------------------------

        orientation = 0;
    }


    // ==================================================
    // EDGE ORIENTIERUNG
    // ==================================================

    private void UpdateEdgeOrientation()
    {
        orientation = 0;


        // ------------------------------------------
        // UR / UF / UL / UB-artige Kanten
        // ------------------------------------------

        foreach (CubieSticker sticker in stickers)
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveY ||
                sticker.originalDirection ==
                    FaceDirection.NegativeY
            )
            {
                if (
                    sticker.currentDirection ==
                        FaceDirection.PositiveY ||
                    sticker.currentDirection ==
                        FaceDirection.NegativeY
                )
                {
                    orientation = 0;
                }
                else
                {
                    orientation = 1;
                }

                return;
            }
        }


        // ------------------------------------------
        // Andere Kanten
        // ------------------------------------------

        foreach (CubieSticker sticker in stickers)
        {
            if (
                sticker.originalDirection ==
                    FaceDirection.PositiveZ ||
                sticker.originalDirection ==
                    FaceDirection.NegativeZ
            )
            {
                if (
                    sticker.currentDirection ==
                        FaceDirection.PositiveZ ||
                    sticker.currentDirection ==
                        FaceDirection.NegativeZ
                )
                {
                    orientation = 0;
                }
                else
                {
                    orientation = 1;
                }

                return;
            }
        }
    }


    // ==================================================
    // LOGISCHE POSITION AUF TRANSFORM ANWENDEN
    // ==================================================

    public void ApplyLogicalPosition()
    {
        transform.localPosition =
            new Vector3(
                logicalPosition.x,
                logicalPosition.y,
                logicalPosition.z
            );
    }


    // ==================================================
    // LOGISCHE ROTATION AUF TRANSFORM ANWENDEN
    // ==================================================

    public void ApplyLogicalRotation()
    {
        transform.localRotation =
            logicalRotation;
    }


    // ==================================================
    // AKTUELLEN ZUSTAND ALS TEXT
    // ==================================================

    public string GetState()
    {
        string stickerText = "";

        foreach (CubieSticker sticker in stickers)
        {
            stickerText +=
                sticker.originalDirection +
                "->" +
                sticker.currentDirection +
                " ";
        }

        return
            pieceID +
            " | " +
            Type +
            " | Pos=" +
            logicalPosition +
            " | Ori=" +
            orientation +
            " | Stickers=" +
            stickerText;
    }
}