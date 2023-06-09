import { CircularProgress } from '@equinor/eds-core-react'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { useErrorHandler } from 'react-error-boundary'
import { PlaceRobotInMap } from '../../../utils/MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { MapMetadata } from 'models/MapMetadata'
import { AssetDeck } from 'models/AssetDeck'

interface AssetDeckProps {
    assetDeck: AssetDeck
}

const StyledMap = styled.canvas`
    object-fit: contain;
    max-height: 100%;
    max-width: 100%;
    margin: auto;
`

const StyledMapLimits = styled.div`
    display: flex;
    max-height: 600px;
    max-width: 600px;
`

const StyledLoading = styled.div`
    display: flex;
    justify-content: center;
`

export function AssetDeckMapView({ assetDeck }: AssetDeckProps) {
    const handleError = useErrorHandler()
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [mapMetadata, setMapMetadata] = useState<MapMetadata>()
    const [imageObjectURL, setImageObjectURL] = useState<string>()
    const [isLoading, setIsLoading] = useState<boolean>()

    const updateMap = () => {
        let context = mapCanvas.getContext('2d')
        if (context === null) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context?.drawImage(mapImage, 0, 0)
        if (mapMetadata) {
            PlaceRobotInMap(mapMetadata, mapCanvas, assetDeck.defaultLocalizationPose)
        }
    }

    const getMeta = async (url: string) => {
        const image = new Image()
        image.src = url
        await image.decode()
        return image
    }

    useEffect(() => {
        setIsLoading(true)
        setImageObjectURL(undefined)
        BackendAPICaller.getAssetDeckMapMetadata(assetDeck.id)
            .then((mapMetadata) => {
                setMapMetadata(mapMetadata)
                BackendAPICaller.getMap(assetDeck.assetCode, mapMetadata.mapName)
                    .then((imageBlob) => {
                        setImageObjectURL(URL.createObjectURL(imageBlob))
                    })
                    .catch(() => {
                        setImageObjectURL(NoMap)
                    })
            })
            .catch(() => {
                setMapMetadata(undefined)
                setImageObjectURL(NoMap)
            })
        //.catch((e) => handleError(e))
    }, [assetDeck])

    useEffect(() => {
        if (!imageObjectURL) {
            return
        }
        getMeta(imageObjectURL).then((img) => {
            const mapCanvas = document.getElementById('mapCanvas') as HTMLCanvasElement
            mapCanvas.width = img.width
            mapCanvas.height = img.height
            let context = mapCanvas?.getContext('2d')
            if (context) {
                setMapContext(context)
                context.drawImage(img, 0, 0)
            }
            setMapCanvas(mapCanvas)
            setMapImage(img)
        })
        setIsLoading(false)
    }, [imageObjectURL])

    useEffect(() => {
        let animationFrameId = 0
        if (mapContext) {
            const render = () => {
                updateMap()
                animationFrameId = window.requestAnimationFrame(render)
            }
            render()
        }
        return () => {
            window.cancelAnimationFrame(animationFrameId)
        }
    }, [updateMap, mapContext])

    return (
        <>
            {isLoading && (
                <StyledLoading>
                    <CircularProgress />
                </StyledLoading>
            )}
            {!isLoading && (
                <StyledMapLimits>
                    <StyledMap id="mapCanvas" />
                </StyledMapLimits>
            )}
        </>
    )
}