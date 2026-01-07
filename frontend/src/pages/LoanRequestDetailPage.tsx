import { useParams, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, MapPin, Calendar, Clock, Users, FileText } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/Card'
import { Badge } from '@/components/ui/Badge'
import { PageLoading } from '@/components/ui/Loading'
import { Alert } from '@/components/ui/Alert'
import { loanRequestService, approvalService, scheduleService } from '@/services'
import { formatDate, formatDateTime } from '@/lib/utils'

export default function LoanRequestDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { data: request, isLoading, error } = useQuery({
    queryKey: ['loan-request', id],
    queryFn: async () => {
      const response = await loanRequestService.getById(Number(id))
      return response.data
    },
    enabled: !!id
  })

  const { data: approvalHistory } = useQuery({
    queryKey: ['approval-history', id],
    queryFn: async () => {
      const response = await approvalService.getHistory(Number(id))
      return response.data || []
    },
    enabled: !!id
  })

  const { data: schedule } = useQuery({
    queryKey: ['schedule-by-request', id],
    queryFn: async () => {
      const response = await scheduleService.getByLoanRequestId(Number(id))
      return response.data
    },
    enabled: !!id && request?.status === 'APPROVED'
  })

  if (isLoading) return <PageLoading />

  if (error || !request) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" onClick={() => navigate(-1)}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Kembali
        </Button>
        <Alert variant="error">
          Pengajuan tidak ditemukan
        </Alert>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate(-1)}>
          <ArrowLeft className="w-4 h-4" />
        </Button>
        <div className="flex-1">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold text-gray-900">Detail Pengajuan</h1>
            <Badge status={request.status} />
          </div>
          <p className="text-gray-600">ID: #{request.id}</p>
        </div>
      </div>

      {/* Main Info */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileText className="w-5 h-5" />
            Informasi Pengajuan
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <div>
                <p className="text-sm text-gray-500">Tujuan Peminjaman</p>
                <p className="font-medium text-gray-900">{request.purpose}</p>
              </div>
              <div className="flex items-start gap-2">
                <MapPin className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Destinasi</p>
                  <p className="font-medium text-gray-900">{request.destination}</p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <Users className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Jumlah Penumpang</p>
                  <p className="font-medium text-gray-900">{request.passengerCount} orang</p>
                </div>
              </div>
            </div>
            <div className="space-y-4">
              <div className="flex items-start gap-2">
                <Calendar className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Tanggal Keberangkatan</p>
                  <p className="font-medium text-gray-900">{formatDate(request.departureDate)}</p>
                </div>
              </div>
              <div className="flex items-start gap-2">
                <Clock className="w-4 h-4 text-gray-400 mt-1" />
                <div>
                  <p className="text-sm text-gray-500">Waktu</p>
                  <p className="font-medium text-gray-900">
                    {request.departureTime} - {request.returnTime}
                  </p>
                </div>
              </div>
              {request.notes && (
                <div>
                  <p className="text-sm text-gray-500">Catatan</p>
                  <p className="text-gray-700">{request.notes}</p>
                </div>
              )}
            </div>
          </div>
          <div className="mt-6 pt-6 border-t">
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-gray-500">Dibuat pada</p>
                <p className="text-gray-900">{formatDateTime(request.createdAt)}</p>
              </div>
              <div>
                <p className="text-gray-500">Terakhir diupdate</p>
                <p className="text-gray-900">{formatDateTime(request.updatedAt)}</p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Approval History */}
      <Card>
        <CardHeader>
          <CardTitle>Riwayat Approval</CardTitle>
        </CardHeader>
        <CardContent>
          {!approvalHistory || approvalHistory.length === 0 ? (
            <p className="text-gray-500 text-center py-4">Belum ada riwayat approval</p>
          ) : (
            <div className="space-y-4">
              {approvalHistory.map((approval, index) => (
                <div 
                  key={approval.id}
                  className="flex gap-4 relative"
                >
                  {/* Timeline Line */}
                  {index < approvalHistory.length - 1 && (
                    <div className="absolute left-3 top-8 w-0.5 h-full bg-gray-200" />
                  )}
                  
                  {/* Timeline Dot */}
                  <div className={`
                    w-6 h-6 rounded-full flex-shrink-0 flex items-center justify-center text-white text-xs
                    ${approval.status === 'APPROVED' ? 'bg-green-500' : 
                      approval.status === 'REJECTED' ? 'bg-red-500' : 'bg-gray-400'}
                  `}>
                    {approval.approvalLevel}
                  </div>
                  
                  {/* Content */}
                  <div className="flex-1 pb-4">
                    <div className="flex items-center justify-between">
                      <p className="font-medium text-gray-900">
                        Approval Level {approval.approvalLevel}
                      </p>
                      <Badge status={approval.status} />
                    </div>
                    <p className="text-sm text-gray-600 mt-1">
                      oleh <span className="font-medium">{approval.approverName}</span>
                    </p>
                    {approval.remarks && (
                      <p className="text-sm text-gray-500 mt-2 bg-gray-50 p-2 rounded">
                        "{approval.remarks}"
                      </p>
                    )}
                    <p className="text-xs text-gray-400 mt-2">
                      {formatDateTime(approval.approvalDate)}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Schedule Info (if approved) */}
      {schedule && (
        <Card className="bg-green-50 border-green-200">
          <CardHeader>
            <CardTitle className="text-green-800">Jadwal Perjalanan</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <p className="text-sm text-green-700">Kendaraan</p>
                <p className="font-medium text-green-900">
                  {schedule.vehiclePlateNumber} - {schedule.vehicleModel}
                </p>
              </div>
              <div>
                <p className="text-sm text-green-700">Driver</p>
                <p className="font-medium text-green-900">{schedule.driverName}</p>
              </div>
              <div>
                <p className="text-sm text-green-700">Status</p>
                <Badge status={schedule.status} />
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
